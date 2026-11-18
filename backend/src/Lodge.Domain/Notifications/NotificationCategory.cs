namespace Lodge.Domain.Notifications;

/// <summary>
/// The notification categories users can turn on or off, per channel (e-mail and in-app).
/// </summary>
public enum NotificationCategory
{
    Messages = 1,
    Reservations = 2,
    Reviews = 3,
    Payouts = 4,
    Reminders = 5,
}
