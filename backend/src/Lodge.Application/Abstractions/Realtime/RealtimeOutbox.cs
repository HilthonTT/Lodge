using System.Data;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Messaging;
using Lodge.Application.Notifications;
using Lodge.Contracts.Messaging;
using Lodge.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace Lodge.Application.Abstractions.Realtime;

/// <summary>
/// Collects the realtime events of a unit of work and pushes them once the changes are committed, so that clients
/// never hear about data they cannot read yet. Commands are flushed by the unit of work pipeline; background writers
/// (event handlers, jobs) call <see cref="FlushAsync"/> after saving.
/// </summary>
public interface IRealtimeOutbox
{
    /// <summary>
    /// A message was posted: both participants get <c>messageReceived</c>, <c>conversationUpdated</c> and new
    /// unread counts.
    /// </summary>
    void MessagePosted(Guid messageId);

    /// <summary>
    /// A conversation changed (e.g. a read marker moved): its participants get <c>conversationUpdated</c> and new
    /// unread counts.
    /// </summary>
    void ConversationChanged(Guid conversationId);

    /// <summary>
    /// A notification was created: its user gets <c>notificationReceived</c> and new unread counts.
    /// </summary>
    void NotificationCreated(Guid notificationId);

    /// <summary>
    /// The user's unread counters changed.
    /// </summary>
    void CountsChanged(Guid userId);

    /// <summary>
    /// Pushes the collected events. Failures are logged, never thrown: realtime delivery is best effort.
    /// </summary>
    Task FlushAsync(CancellationToken cancellationToken = default);
}

internal sealed partial class RealtimeOutbox(
    IDbConnectionFactory connectionFactory,
    IRealtimeNotifier notifier,
    ILogger<RealtimeOutbox> logger) : IRealtimeOutbox
{
    private readonly List<Guid> _messages = [];
    private readonly List<Guid> _conversations = [];
    private readonly List<Guid> _notifications = [];
    private readonly HashSet<Guid> _users = [];

    public void MessagePosted(Guid messageId) => _messages.Add(messageId);

    public void ConversationChanged(Guid conversationId) => _conversations.Add(conversationId);

    public void NotificationCreated(Guid notificationId) => _notifications.Add(notificationId);

    public void CountsChanged(Guid userId) => _users.Add(userId);

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_messages.Count == 0 && _conversations.Count == 0 && _notifications.Count == 0 && _users.Count == 0)
        {
            return;
        }

        Guid[] messages = [.. _messages.Distinct()];
        Guid[] conversations = [.. _conversations.Distinct()];
        Guid[] notifications = [.. _notifications.Distinct()];
        HashSet<Guid> users = [.. _users];

        _messages.Clear();
        _conversations.Clear();
        _notifications.Clear();
        _users.Clear();

        try
        {
            using IDbConnection connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken);

            foreach (Guid messageId in messages)
            {
                MessagingQueries.MessageWithParticipants? message =
                    await MessagingQueries.GetMessageWithParticipantsAsync(connection, messageId);

                if (message is null)
                {
                    continue;
                }

                Guid[] participants = [message.GuestId, message.HostId];

                await notifier.MessageReceivedAsync(participants, message.Message.ConversationId, message.Message, cancellationToken);
                await notifier.ConversationUpdatedAsync(participants, message.Message.ConversationId, cancellationToken);

                users.UnionWith(participants);
            }

            foreach (Guid conversationId in conversations)
            {
                (Guid GuestId, Guid HostId)? participants =
                    await MessagingQueries.GetParticipantsAsync(connection, conversationId);

                if (participants is null)
                {
                    continue;
                }

                Guid[] userIds = [participants.Value.GuestId, participants.Value.HostId];

                await notifier.ConversationUpdatedAsync(userIds, conversationId, cancellationToken);

                users.UnionWith(userIds);
            }

            foreach (Guid notificationId in notifications)
            {
                (Guid UserId, NotificationResponse Notification)? notification =
                    await NotificationQueries.GetForPushAsync(connection, notificationId);

                if (notification is null)
                {
                    continue;
                }

                await notifier.NotificationReceivedAsync(notification.Value.UserId, notification.Value.Notification, cancellationToken);

                users.Add(notification.Value.UserId);
            }

            foreach (Guid userId in users)
            {
                UnreadCountsResponse counts = await NotificationQueries.GetUnreadCountsAsync(connection, userId);

                await notifier.UnreadCountsAsync(userId, counts, cancellationToken);
            }
        }
#pragma warning disable CA1031 // Realtime delivery is best effort: the data is already committed.
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogPushFailed(logger, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to push realtime events")]
    private static partial void LogPushFailed(ILogger logger, Exception exception);
}
