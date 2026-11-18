using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Bookings.Events;

/// <summary>
/// Raised when a guest requests to book a listing that requires the host's approval.
/// </summary>
/// <param name="BookingId">The booking (request) identifier.</param>
public sealed record BookingRequestedDomainEvent(Guid BookingId) : IDomainEvent;
