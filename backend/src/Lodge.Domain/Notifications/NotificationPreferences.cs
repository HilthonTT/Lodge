using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Notifications;

/// <summary>
/// A user's notification settings per category and channel. Users without a row get everything (all true).
/// Transactional security e-mails (e-mail verification, password reset, password changed) ignore these settings.
/// </summary>
public sealed class NotificationPreferences : Entity
{
    private NotificationPreferences(Guid userId)
        : base(userId)
    {
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private NotificationPreferences()
    {
    }

    /// <summary>
    /// Gets the user identifier (also the entity identifier).
    /// </summary>
    public Guid UserId => Id;

    public bool EmailMessages { get; private set; } = true;

    public bool EmailReservations { get; private set; } = true;

    public bool EmailReviews { get; private set; } = true;

    public bool EmailPayouts { get; private set; } = true;

    public bool EmailReminders { get; private set; } = true;

    public bool InAppMessages { get; private set; } = true;

    public bool InAppReservations { get; private set; } = true;

    public bool InAppReviews { get; private set; } = true;

    public bool InAppPayouts { get; private set; } = true;

    public bool InAppReminders { get; private set; } = true;

    public DateTime? ModifiedOnUtc { get; private set; }

    /// <summary>
    /// Creates the default settings (everything on).
    /// </summary>
    public static NotificationPreferences CreateDefault(Guid userId) => new(userId);

    public bool AllowsEmail(NotificationCategory category) => category switch
    {
        NotificationCategory.Messages => EmailMessages,
        NotificationCategory.Reservations => EmailReservations,
        NotificationCategory.Reviews => EmailReviews,
        NotificationCategory.Payouts => EmailPayouts,
        NotificationCategory.Reminders => EmailReminders,
        _ => true,
    };

    public bool AllowsInApp(NotificationCategory category) => category switch
    {
        NotificationCategory.Messages => InAppMessages,
        NotificationCategory.Reservations => InAppReservations,
        NotificationCategory.Reviews => InAppReviews,
        NotificationCategory.Payouts => InAppPayouts,
        NotificationCategory.Reminders => InAppReminders,
        _ => true,
    };

    public void SetEmail(bool messages, bool reservations, bool reviews, bool payouts, bool reminders)
    {
        EmailMessages = messages;
        EmailReservations = reservations;
        EmailReviews = reviews;
        EmailPayouts = payouts;
        EmailReminders = reminders;
    }

    public void SetInApp(bool messages, bool reservations, bool reviews, bool payouts, bool reminders)
    {
        InAppMessages = messages;
        InAppReservations = reservations;
        InAppReviews = reviews;
        InAppPayouts = payouts;
        InAppReminders = reminders;
    }

    public void Touch(DateTime utcNow) => ModifiedOnUtc = utcNow;
}
