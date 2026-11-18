using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Bookings.Events;

/// <summary>
/// Raised when the host accepts a booking request (together with <see cref="BookingReservedDomainEvent"/>).
/// </summary>
/// <param name="BookingId">The booking identifier.</param>
public sealed record BookingAcceptedDomainEvent(Guid BookingId) : IDomainEvent;
