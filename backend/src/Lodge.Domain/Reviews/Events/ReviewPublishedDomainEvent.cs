using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Reviews.Events;

/// <summary>
/// Raised when a guest's review of a listing is published (both sides reviewed, or the review window closed).
/// </summary>
/// <param name="ReviewId">The review identifier.</param>
public sealed record ReviewPublishedDomainEvent(Guid ReviewId) : IDomainEvent;
