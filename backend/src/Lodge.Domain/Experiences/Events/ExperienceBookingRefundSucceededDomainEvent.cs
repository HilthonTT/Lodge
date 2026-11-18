using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Experiences.Events;

/// <summary>
/// Raised when the refund of an experience booking succeeded.
/// </summary>
public sealed record ExperienceBookingRefundSucceededDomainEvent(Guid ExperienceBookingId) : IDomainEvent;
