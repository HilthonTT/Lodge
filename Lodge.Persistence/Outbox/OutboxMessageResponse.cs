namespace Lodge.Persistence.Outbox;

/// <summary>
/// Represents the outbox message response.
/// This response is the simplified version of <see cref="OutboxMessage"/> record.
/// It it used only in the <see cref="ProcessOutboxMessagesJob"/> class for processing purposes.
/// </summary>
/// <param name="Id">The outbox message id.</param>
/// <param name="Content">The outbox message content.</param>
internal sealed record OutboxMessageResponse(Guid Id, string Content);