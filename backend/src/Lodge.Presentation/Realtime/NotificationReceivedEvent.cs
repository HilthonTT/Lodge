using Lodge.Contracts.Notifications;

namespace Lodge.Presentation.Realtime;

/// <summary>
/// The payload of <c>notificationReceived</c>.
/// </summary>
public sealed record NotificationReceivedEvent(NotificationResponse Notification);
