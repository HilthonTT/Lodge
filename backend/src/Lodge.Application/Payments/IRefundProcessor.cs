using Lodge.Domain.Bookings;

namespace Lodge.Application.Payments;

/// <summary>
/// Sends the pending refund of a cancelled booking (called by the integration event consumer).
/// </summary>
public interface IRefundProcessor
{
    /// <summary>
    /// Refunds the booking through Stripe (idempotency key = booking id) or simulates it, and records the outcome
    /// (<see cref="RefundStatus.Succeeded"/> or <see cref="RefundStatus.Failed"/>). Does nothing unless the refund
    /// is <see cref="RefundStatus.Pending"/>.
    /// </summary>
    Task ProcessAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refunds a cancelled experience booking the same way (idempotency key = experience booking id).
    /// </summary>
    Task ProcessExperienceAsync(Guid experienceBookingId, CancellationToken cancellationToken = default);
}
