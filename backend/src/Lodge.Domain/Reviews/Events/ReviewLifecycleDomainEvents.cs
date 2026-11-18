using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Reviews.Events;

/// <summary>
/// Raised when a guest's review of a listing is published (both sides reviewed, or the review window closed).
/// </summary>
/// <param name="ReviewId">The review identifier.</param>
public sealed record ReviewPublishedDomainEvent(Guid ReviewId) : IDomainEvent;

/// <summary>
/// Raised when a host's review of a guest is published.
/// </summary>
/// <param name="GuestReviewId">The guest review identifier.</param>
public sealed record GuestReviewPublishedDomainEvent(Guid GuestReviewId) : IDomainEvent;

/// <summary>
/// Raised when the host replies to a published review for the first time (edits raise nothing).
/// </summary>
/// <param name="ReviewId">The review identifier.</param>
public sealed record ReviewRepliedDomainEvent(Guid ReviewId) : IDomainEvent;
