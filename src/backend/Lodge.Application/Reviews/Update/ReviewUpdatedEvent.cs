using MediatR;

namespace Lodge.Application.Reviews.Update;

/// <summary>
/// Represents the event that is triggered when a review has been updated.
/// </summary>
/// <param name="ReviewId">The review identifier.</param>
internal sealed record ReviewUpdatedEvent(Guid ReviewId) : INotification;
