using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Experiences.Events;

/// <summary>
/// Raised when an experience booking is paid and confirmed.
/// </summary>
public sealed record ExperienceBookingConfirmedDomainEvent(Guid ExperienceBookingId) : IDomainEvent;

/// <summary>
/// Raised when an experience booking is cancelled by the guest or the host.
/// </summary>
public sealed record ExperienceBookingCancelledDomainEvent(Guid ExperienceBookingId) : IDomainEvent;

/// <summary>
/// Raised when a cancelled, paid experience booking has a refund to send.
/// </summary>
public sealed record ExperienceBookingRefundRequestedDomainEvent(Guid ExperienceBookingId) : IDomainEvent;

/// <summary>
/// Raised when the refund of an experience booking succeeded.
/// </summary>
public sealed record ExperienceBookingRefundSucceededDomainEvent(Guid ExperienceBookingId) : IDomainEvent;
