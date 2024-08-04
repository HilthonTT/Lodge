using MediatR;

namespace Lodge.Application.Reviews.Create;

/// <summary>
/// Represents the event that is triggered when a review is created.
/// </summary>
/// <param name="ReviewId">The review identifier.</param>
internal sealed record ReviewCreatedEvent(Guid ReviewId) : INotification;
