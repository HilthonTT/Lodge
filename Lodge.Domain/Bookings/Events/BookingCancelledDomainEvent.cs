using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Bookings.Events;

/// <summary>
/// Represents the event that is raised when a booking is cancelled.
/// </summary>
/// <param name="BookingId">The cancelled booking's id.</param>
public sealed record BookingCancelledDomainEvent(Guid BookingId) : IDomainEvent;
