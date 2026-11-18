using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Bookings.Events;

/// <summary>
/// Raised when the guest moves a pending or reserved booking to new dates.
/// </summary>
/// <param name="BookingId">The booking identifier.</param>
/// <param name="PreviousStart">The previous check-in date.</param>
/// <param name="PreviousEnd">The previous check-out date.</param>
public sealed record BookingDatesChangedDomainEvent(Guid BookingId, DateOnly PreviousStart, DateOnly PreviousEnd)
    : IDomainEvent;
