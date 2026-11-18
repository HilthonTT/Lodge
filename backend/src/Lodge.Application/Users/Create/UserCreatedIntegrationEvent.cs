using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Users.Create;

/// <summary>
/// Published to the message broker when a user has registered.
/// </summary>
public sealed record UserCreatedIntegrationEvent(Guid UserId) : IIntegrationEvent;
