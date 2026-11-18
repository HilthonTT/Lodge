using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Reviews.Events;

/// <summary>
/// Raised when a host's review of a guest is published.
/// </summary>
/// <param name="GuestReviewId">The guest review identifier.</param>
public sealed record GuestReviewPublishedDomainEvent(Guid GuestReviewId) : IDomainEvent;
