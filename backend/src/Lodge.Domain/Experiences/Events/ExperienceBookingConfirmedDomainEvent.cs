using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Experiences.Events;

/// <summary>
/// Raised when an experience booking is paid and confirmed.
/// </summary>
public sealed record ExperienceBookingConfirmedDomainEvent(Guid ExperienceBookingId) : IDomainEvent;
