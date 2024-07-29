using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Users.Events;
using Newtonsoft.Json;

namespace Lodge.Application.Users.ChangePassword;

/// <summary>
/// Represents the integration event that is raised when a user's password is changed.
/// </summary>
public sealed class UserPasswordChangedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserPasswordChangedIntegrationEvent"/> class.
    /// </summary>
    internal UserPasswordChangedIntegrationEvent(UserPasswordChangedDomainEvent userPasswordChangedDomainEvent)
    {
        UserId = userPasswordChangedDomainEvent.User.Id;
    }

    [JsonConstructor]
    private UserPasswordChangedIntegrationEvent(Guid userId) => UserId = userId;

    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public Guid UserId { get; }
}
