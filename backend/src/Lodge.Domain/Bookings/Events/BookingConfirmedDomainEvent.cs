using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Bookings.Events;

/// <summary>
/// Represents the event that is raised when a booking is confirmed.
/// </summary>
/// <param name="BookingId">The confirmed booking's id.</param>
public sealed record BookingConfirmedDomainEvent(Guid BookingId) : IDomainEvent;
