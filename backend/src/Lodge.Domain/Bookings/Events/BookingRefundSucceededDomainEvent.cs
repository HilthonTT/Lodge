using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Bookings.Events;

/// <summary>
/// Raised when the refund of a cancelled booking succeeded.
/// </summary>
/// <param name="BookingId">The booking identifier.</param>
public sealed record BookingRefundSucceededDomainEvent(Guid BookingId) : IDomainEvent;
