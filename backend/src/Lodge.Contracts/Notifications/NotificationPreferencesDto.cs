namespace Lodge.Contracts.Notifications;

/// <summary>
/// The notification settings (<c>GET</c>/<c>PUT /users/{userId}/notification-preferences</c>).
/// </summary>
public sealed record NotificationPreferencesDto(NotificationChannelDto Email, NotificationChannelDto InApp);
