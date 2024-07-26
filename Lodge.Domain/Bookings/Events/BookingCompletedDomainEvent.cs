using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Bookings.Events;

/// <summary>
/// Represents the event that is raised when a booking is completed.
/// </summary>
/// <param name="BookingId">The completed booking's id.</param>
public sealed record BookingCompletedDomainEvent(Guid BookingId) : IDomainEvent;
