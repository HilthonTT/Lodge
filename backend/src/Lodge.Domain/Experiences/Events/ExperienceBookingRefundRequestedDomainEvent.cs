using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Experiences.Events;

/// <summary>
/// Raised when a cancelled, paid experience booking has a refund to send.
/// </summary>
public sealed record ExperienceBookingRefundRequestedDomainEvent(Guid ExperienceBookingId) : IDomainEvent;
