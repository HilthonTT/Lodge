using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Users.Events;
using Newtonsoft.Json;

namespace Lodge.Application.Users.Create;

/// <summary>
/// Represents the integration event that is raised when a user is created.
/// </summary>
public sealed class UserCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserCreatedIntegrationEvent"/> class.
    /// </summary>
    /// <param name="userCreatedDomainEvent">
    /// The user created domain event.
    /// </param>
    internal UserCreatedIntegrationEvent(UserCreatedDomainEvent userCreatedDomainEvent)
    {
        UserId = userCreatedDomainEvent.UserId;
    }

    [JsonConstructor]
    private UserCreatedIntegrationEvent(Guid userId) => UserId = userId;

    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public Guid UserId { get; }
}
