using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Messaging.Events;

/// <summary>
/// Raised when a participant sends a message (not for system messages).
/// </summary>
/// <param name="MessageId">The message identifier.</param>
public sealed record MessageSentDomainEvent(Guid MessageId) : IDomainEvent;
