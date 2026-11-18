namespace Lodge.Contracts.Notifications;

/// <summary>
/// The categories of one channel; all true by default.
/// </summary>
public sealed record NotificationChannelDto(bool Messages, bool Reservations, bool Reviews, bool Payouts, bool Reminders);
