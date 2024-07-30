using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Reviews.Events;

/// <summary>
/// Represents the event that is raised when a review is created.
/// </summary>
/// <param name="ReviewId">The created review's id.</param>
public sealed record ReviewCreatedDomainEvent(Guid ReviewId) : IDomainEvent;
