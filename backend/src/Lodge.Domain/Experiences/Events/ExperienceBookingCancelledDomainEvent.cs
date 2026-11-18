using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Experiences.Events;

/// <summary>
/// Raised when an experience booking is cancelled by the guest or the host.
/// </summary>
public sealed record ExperienceBookingCancelledDomainEvent(Guid ExperienceBookingId) : IDomainEvent;
