using System.Data;
using FluentValidation;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Realtime;
using Lodge.Application.Core.Extensions;
using Lodge.Contracts.Common;
using Lodge.Contracts.Messaging;
using Lodge.Contracts.Notifications;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Notifications;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Application.Notifications;

/// <summary>
/// Gets one page of the caller's in-app notifications, newest first (<c>GET /notifications</c>).
/// </summary>
public sealed record GetNotificationsQuery(bool UnreadOnly, int Page, int PageSize) : IQuery<PagedList<NotificationResponse>>;

internal sealed class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}

internal sealed class GetNotificationsQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory) : IQueryHandler<GetNotificationsQuery, PagedList<NotificationResponse>>
{
    public async Task<Result<PagedList<NotificationResponse>>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<NotificationResponse> items, int totalCount) = await NotificationQueries.GetPageAsync(
            connection,
            userIdentifierProvider.UserId,
            request.UnreadOnly,
            request.Page,
            request.PageSize);

        return new PagedList<NotificationResponse>(items, request.Page, request.PageSize, totalCount);
    }
}

/// <summary>
/// Counts the caller's unread in-app notifications (<c>GET /notifications/unread-count</c>).
/// </summary>
public sealed record GetUnreadNotificationCountQuery : IQuery<UnreadCountResponse>;

internal sealed class GetUnreadNotificationCountQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory) : IQueryHandler<GetUnreadNotificationCountQuery, UnreadCountResponse>
{
    public async Task<Result<UnreadCountResponse>> Handle(
        GetUnreadNotificationCountQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        return new UnreadCountResponse(await NotificationQueries.CountUnreadAsync(connection, userIdentifierProvider.UserId));
    }
}

/// <summary>
/// Gets both unread counters of a user (sent to each new SignalR connection).
/// </summary>
public sealed record GetUnreadCountsQuery(Guid UserId) : IQuery<UnreadCountsResponse>;

internal sealed class GetUnreadCountsQueryHandler(IDbConnectionFactory factory)
    : IQueryHandler<GetUnreadCountsQuery, UnreadCountsResponse>
{
    public async Task<Result<UnreadCountsResponse>> Handle(GetUnreadCountsQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        return await NotificationQueries.GetUnreadCountsAsync(connection, request.UserId);
    }
}

/// <summary>
/// Marks one of the caller's notifications as read (<c>PUT /notifications/{id}/read</c>); other users' notifications
/// are "not found".
/// </summary>
public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand;

internal sealed class MarkNotificationReadCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbContext context,
    IRealtimeOutbox realtimeOutbox,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        Notification? notification = await context.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == userId && n.InApp, cancellationToken);

        if (notification is null)
        {
            return Result.Failure(NotificationErrors.NotFound(request.NotificationId));
        }

        notification.MarkRead(dateTimeProvider.UtcNow);
        realtimeOutbox.CountsChanged(userId);

        return Result.Success();
    }
}

/// <summary>
/// Marks all of the caller's notifications as read (<c>PUT /notifications/read-all</c>).
/// </summary>
public sealed record MarkAllNotificationsReadCommand : ICommand;

internal sealed class MarkAllNotificationsReadCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbContext context,
    IRealtimeOutbox realtimeOutbox,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<MarkAllNotificationsReadCommand>
{
    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;
        DateTime utcNow = dateTimeProvider.UtcNow;

        await context.Set<Notification>()
            .Where(n => n.UserId == userId && n.ReadOnUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.ReadOnUtc, utcNow), cancellationToken);

        realtimeOutbox.CountsChanged(userId);

        return Result.Success();
    }
}

/// <summary>
/// Gets the caller's notification preferences (<c>GET /users/{userId}/notification-preferences</c>).
/// </summary>
public sealed record GetNotificationPreferencesQuery(Guid UserId) : IQuery<NotificationPreferencesDto>;

internal sealed class GetNotificationPreferencesQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    INotificationPreferencesProvider preferencesProvider)
    : IQueryHandler<GetNotificationPreferencesQuery, NotificationPreferencesDto>
{
    public async Task<Result<NotificationPreferencesDto>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<NotificationPreferencesDto>(UserErrors.InvalidPermissions);
        }

        return NotificationPreferencesMapper.ToDto(await preferencesProvider.GetAsync(request.UserId, cancellationToken));
    }
}

/// <summary>
/// Replaces the caller's notification preferences (<c>PUT /users/{userId}/notification-preferences</c>).
/// </summary>
public sealed record UpdateNotificationPreferencesCommand(Guid UserId, NotificationPreferencesDto Preferences)
    : ICommand<NotificationPreferencesDto>;

internal sealed class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.Preferences).NotNull();
        RuleFor(x => x.Preferences.Email).NotNull().When(x => x.Preferences is not null);
        RuleFor(x => x.Preferences.InApp).NotNull().When(x => x.Preferences is not null);
    }
}

internal sealed class UpdateNotificationPreferencesCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbContext context,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateNotificationPreferencesCommand, NotificationPreferencesDto>
{
    public async Task<Result<NotificationPreferencesDto>> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<NotificationPreferencesDto>(UserErrors.InvalidPermissions);
        }

        NotificationPreferences? preferences = await context.Set<NotificationPreferences>()
            .FirstOrDefaultAsync(p => p.Id == request.UserId, cancellationToken);

        if (preferences is null)
        {
            preferences = NotificationPreferences.CreateDefault(request.UserId);
            context.Set<NotificationPreferences>().Add(preferences);
        }

        NotificationChannelDto email = request.Preferences.Email;
        NotificationChannelDto inApp = request.Preferences.InApp;

        preferences.SetEmail(email.Messages, email.Reservations, email.Reviews, email.Payouts, email.Reminders);
        preferences.SetInApp(inApp.Messages, inApp.Reservations, inApp.Reviews, inApp.Payouts, inApp.Reminders);
        preferences.Touch(dateTimeProvider.UtcNow);

        return NotificationPreferencesMapper.ToDto(preferences);
    }
}

internal static class NotificationPreferencesMapper
{
    public static NotificationPreferencesDto ToDto(NotificationPreferences preferences) => new(
        new NotificationChannelDto(
            preferences.EmailMessages,
            preferences.EmailReservations,
            preferences.EmailReviews,
            preferences.EmailPayouts,
            preferences.EmailReminders),
        new NotificationChannelDto(
            preferences.InAppMessages,
            preferences.InAppReservations,
            preferences.InAppReviews,
            preferences.InAppPayouts,
            preferences.InAppReminders));
}
