using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Bookings.Events;

/// <summary>
/// Represents the event that is raised when a booking is created.
/// </summary>
/// <param name="BookingId">The reserved booking's id.</param>
public sealed record BookingReservedDomainEvent(Guid BookingId) : IDomainEvent;
