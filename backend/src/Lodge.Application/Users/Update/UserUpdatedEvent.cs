using MediatR;

namespace Lodge.Application.Users.Update;

/// <summary>
/// Represents the event that is triggered when a user has been updated.
/// </summary>
/// <param name="UserId">The user identifier.</param>
internal sealed record UserUpdatedEvent(Guid UserId) : INotification;
