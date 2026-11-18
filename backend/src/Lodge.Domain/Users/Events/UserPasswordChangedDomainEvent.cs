using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Users.Events;

/// <summary>
/// Raised when a user changes their password.
/// </summary>
public sealed record UserPasswordChangedDomainEvent(Guid UserId) : IDomainEvent;
