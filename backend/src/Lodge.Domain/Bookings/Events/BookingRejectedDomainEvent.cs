using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Bookings.Events;

/// <summary>
/// Represents the event that is raised when a booking is rejected.
/// </summary>
/// <param name="BookingId">The rejected booking's id.</param>
public sealed record BookingRejectedDomainEvent(Guid BookingId) : IDomainEvent;
