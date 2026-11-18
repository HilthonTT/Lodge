using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Bookings.Events;

/// <summary>
/// Raised when a cancelled, paid booking has a refund to request from the payment provider.
/// </summary>
/// <param name="BookingId">The booking identifier.</param>
public sealed record BookingRefundRequestedDomainEvent(Guid BookingId) : IDomainEvent;
