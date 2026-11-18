using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Users.Events;

/// <summary>
/// Raised when a user changes their first or last name.
/// </summary>
public sealed record UserNameChangedDomainEvent(Guid UserId) : IDomainEvent;
