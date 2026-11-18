using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Users.ChangePassword;

/// <summary>
/// Published to the message broker when a user has changed their password.
/// </summary>
public sealed record UserPasswordChangedIntegrationEvent(Guid UserId) : IIntegrationEvent;
