using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Notifications;

namespace Lodge.Application.Notifications.GetPreferences;

/// <summary>
/// Gets the caller's notification preferences (<c>GET /users/{userId}/notification-preferences</c>).
/// </summary>
public sealed record GetNotificationPreferencesQuery(Guid UserId) : IQuery<NotificationPreferencesDto>;
