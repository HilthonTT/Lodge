namespace Lodge.Domain.Notifications;

/// <summary>
/// The notification types (<see cref="Notification.Type"/>) and their categories.
/// </summary>
public static class NotificationTypes
{
    /// <summary>
    /// To the host: a guest requested to book.
    /// </summary>
    public const string BookingRequested = "BookingRequested";

    /// <summary>
    /// To the guest: the host accepted the request.
    /// </summary>
    public const string BookingAccepted = "BookingAccepted";

    /// <summary>
    /// To the guest: the host (or an admin) declined the request.
    /// </summary>
    public const string BookingDeclined = "BookingDeclined";

    /// <summary>
    /// To the guest: the host did not answer the request in time.
    /// </summary>
    public const string BookingExpired = "BookingExpired";

    /// <summary>
    /// To the guest and the host: the booking is paid and confirmed.
    /// </summary>
    public const string BookingConfirmed = "BookingConfirmed";

    /// <summary>
    /// To the other party: the guest or the host cancelled.
    /// </summary>
    public const string BookingCancelled = "BookingCancelled";

    /// <summary>
    /// To the host: the guest changed the dates.
    /// </summary>
    public const string BookingDatesChanged = "BookingDatesChanged";

    /// <summary>
    /// To the reviewee (host for a listing review, guest for a guest review) once the review is published.
    /// </summary>
    public const string ReviewReceived = "ReviewReceived";

    /// <summary>
    /// To the guest: the host replied to their review.
    /// </summary>
    public const string ReviewReplyReceived = "ReviewReplyReceived";

    /// <summary>
    /// To the host: a payout was sent.
    /// </summary>
    public const string PayoutPaid = "PayoutPaid";

    /// <summary>
    /// To the guest: a refund was sent.
    /// </summary>
    public const string RefundSucceeded = "RefundSucceeded";

    /// <summary>
    /// To the guest, two days before check-in.
    /// </summary>
    public const string TripReminder = "TripReminder";

    /// <summary>
    /// To the host, the day before a guest arrives.
    /// </summary>
    public const string ArrivalReminder = "ArrivalReminder";

    /// <summary>
    /// To the guest and the host, the day after check-out, when they can still review.
    /// </summary>
    public const string ReviewReminder = "ReviewReminder";

    /// <summary>
    /// To the guest and the host: an experience booking is paid and confirmed.
    /// </summary>
    public const string ExperienceBooked = "ExperienceBooked";

    /// <summary>
    /// To the guest, the day before an experience.
    /// </summary>
    public const string ExperienceReminder = "ExperienceReminder";

    private static readonly Dictionary<string, NotificationCategory> Categories = new(StringComparer.Ordinal)
    {
        [BookingRequested] = NotificationCategory.Reservations,
        [BookingAccepted] = NotificationCategory.Reservations,
        [BookingDeclined] = NotificationCategory.Reservations,
        [BookingExpired] = NotificationCategory.Reservations,
        [BookingConfirmed] = NotificationCategory.Reservations,
        [BookingCancelled] = NotificationCategory.Reservations,
        [BookingDatesChanged] = NotificationCategory.Reservations,
        [RefundSucceeded] = NotificationCategory.Reservations,
        [ExperienceBooked] = NotificationCategory.Reservations,
        [ReviewReceived] = NotificationCategory.Reviews,
        [ReviewReplyReceived] = NotificationCategory.Reviews,
        [PayoutPaid] = NotificationCategory.Payouts,
        [TripReminder] = NotificationCategory.Reminders,
        [ArrivalReminder] = NotificationCategory.Reminders,
        [ReviewReminder] = NotificationCategory.Reminders,
        [ExperienceReminder] = NotificationCategory.Reminders,
    };

    public static IReadOnlyCollection<string> All => Categories.Keys;

    public static NotificationCategory CategoryOf(string type) =>
        Categories.TryGetValue(type, out NotificationCategory category)
            ? category
            : throw new ArgumentException($"Unknown notification type '{type}'.", nameof(type));
}
