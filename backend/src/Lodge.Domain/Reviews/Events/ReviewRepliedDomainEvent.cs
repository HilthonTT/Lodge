using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Reviews.Events;

/// <summary>
/// Raised when the host replies to a published review for the first time (edits raise nothing).
/// </summary>
/// <param name="ReviewId">The review identifier.</param>
public sealed record ReviewRepliedDomainEvent(Guid ReviewId) : IDomainEvent;
