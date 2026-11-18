using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Notifications;

namespace Lodge.Application.Notifications;

/// <summary>
/// Replaces the caller's notification preferences (<c>PUT /users/{userId}/notification-preferences</c>).
/// </summary>
public sealed record UpdateNotificationPreferencesCommand(Guid UserId, NotificationPreferencesDto Preferences)
    : ICommand<NotificationPreferencesDto>;
