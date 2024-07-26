using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Users.Events;

public sealed record UserPasswordChangedDomainEvent(User user) : IDomainEvent;
