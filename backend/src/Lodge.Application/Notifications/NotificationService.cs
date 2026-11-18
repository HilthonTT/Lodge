using System.Data;
using System.Text.Json;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Realtime;
using Lodge.Contracts.Notifications;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lodge.Application.Notifications;

/// <summary>
/// An in-app notification to create.
/// </summary>
/// <param name="UserId">The recipient.</param>
/// <param name="Type">One of <see cref="NotificationTypes"/>.</param>
/// <param name="Data">An object serialized as the JSON <c>data</c> (camelCase property names).</param>
/// <param name="Link">The relative frontend path to open.</param>
/// <param name="DedupeKey">Identifies the source event: a second request with the same key is ignored.</param>
public sealed record NotificationRequest(Guid UserId, string Type, object Data, string Link, string DedupeKey);

/// <summary>
/// Creates in-app notifications (once per source event) and pushes them to the user's browsers.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates the notification unless one with the same dedupe key exists. Notifications of a category the user
    /// turned off in-app are recorded but hidden.
    /// </summary>
    /// <returns><see langword="true"/> when the notification is new (callers send their e-mail only then).</returns>
    Task<bool> NotifyAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Reads the users' notification preferences (defaults: everything on).
/// </summary>
public interface INotificationPreferencesProvider
{
    Task<NotificationPreferences> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> AllowsEmailAsync(Guid userId, NotificationCategory category, CancellationToken cancellationToken = default);
}

internal sealed class NotificationPreferencesProvider(IDbContext context) : INotificationPreferencesProvider
{
    public async Task<NotificationPreferences> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.Set<NotificationPreferences>()
            .AsNoTracking()
            .FirstOrDefaultAsync(preferences => preferences.Id == userId, cancellationToken)
        ?? NotificationPreferences.CreateDefault(userId);

    public async Task<bool> AllowsEmailAsync(
        Guid userId,
        NotificationCategory category,
        CancellationToken cancellationToken = default) =>
        (await GetAsync(userId, cancellationToken)).AllowsEmail(category);
}

internal sealed partial class NotificationService(
    IDbConnectionFactory connectionFactory,
    INotificationPreferencesProvider preferencesProvider,
    IRealtimeNotifier realtimeNotifier,
    IDateTimeProvider dateTimeProvider,
    ILogger<NotificationService> logger) : INotificationService
{
    internal static readonly JsonSerializerOptions DataSerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<bool> NotifyAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        NotificationCategory category = NotificationTypes.CategoryOf(request.Type);
        NotificationPreferences preferences = await preferencesProvider.GetAsync(request.UserId, cancellationToken);
        bool inApp = preferences.AllowsInApp(category);

        var notification = Notification.Create(
            request.UserId,
            request.Type,
            JsonSerializer.Serialize(request.Data, DataSerializerOptions),
            request.Link,
            request.DedupeKey.Length > Notification.DedupeKeyMaxLength
                ? request.DedupeKey[..Notification.DedupeKeyMaxLength]
                : request.DedupeKey,
            inApp,
            dateTimeProvider.UtcNow);

        using IDbConnection connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken);

        bool inserted = await NotificationQueries.TryInsertAsync(connection, notification);

        if (inserted && inApp)
        {
            await PushAsync(connection, notification.Id, request.UserId, cancellationToken);
        }

        return inserted;
    }

    private async Task PushAsync(IDbConnection connection, Guid notificationId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            (Guid UserId, NotificationResponse Notification)? pushed =
                await NotificationQueries.GetForPushAsync(connection, notificationId);

            if (pushed is not null)
            {
                await realtimeNotifier.NotificationReceivedAsync(userId, pushed.Value.Notification, cancellationToken);
            }

            await realtimeNotifier.UnreadCountsAsync(
                userId,
                await NotificationQueries.GetUnreadCountsAsync(connection, userId),
                cancellationToken);
        }
#pragma warning disable CA1031 // Realtime delivery is best effort: the notification is stored.
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogPushFailed(logger, exception, notificationId);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to push notification {NotificationId}")]
    private static partial void LogPushFailed(ILogger logger, Exception exception, Guid notificationId);
}
