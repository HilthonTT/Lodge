using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Users.Events;

public sealed record UserNameChangedDomainEvent(User user) : IDomainEvent;
