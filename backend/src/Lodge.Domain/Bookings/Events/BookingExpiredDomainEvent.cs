using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Bookings.Events;

/// <summary>
/// Raised when a booking request expires because the host did not respond in time.
/// </summary>
/// <param name="BookingId">The booking (request) identifier.</param>
public sealed record BookingExpiredDomainEvent(Guid BookingId) : IDomainEvent;
