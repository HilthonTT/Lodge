namespace Lodge.Application.Notifications;

/// <summary>
/// Turns the platform's events into in-app notifications (and the e-mails that have no handler of their own). Called by
/// the integration event handlers of the background tasks; every method is idempotent per source event.
/// </summary>
public interface IUserNotifier
{
    Task BookingRequestedAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task BookingAcceptedAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task BookingDeclinedAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task BookingExpiredAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task BookingConfirmedAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task BookingCancelledAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task BookingDatesChangedAsync(
        Guid bookingId,
        DateOnly previousStart,
        DateOnly previousEnd,
        CancellationToken cancellationToken = default);

    Task BookingRefundSucceededAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task ReviewPublishedAsync(Guid reviewId, CancellationToken cancellationToken = default);

    Task GuestReviewPublishedAsync(Guid guestReviewId, CancellationToken cancellationToken = default);

    Task ReviewRepliedAsync(Guid reviewId, CancellationToken cancellationToken = default);

    Task PayoutPaidAsync(Guid payoutId, CancellationToken cancellationToken = default);

    Task ExperienceBookingConfirmedAsync(Guid experienceBookingId, CancellationToken cancellationToken = default);

    Task ExperienceRefundSucceededAsync(Guid experienceBookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// E-mails the recipient of a new message (if they allow message e-mails, have not read it yet and were not
    /// e-mailed about this conversation in the last 15 minutes).
    /// </summary>
    Task MessageSentAsync(Guid messageId, CancellationToken cancellationToken = default);
}
