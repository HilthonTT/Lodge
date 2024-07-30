namespace Lodge.Persistence.Outbox;

/// <summary>
/// Represents the outbox message entity.
/// </summary>
/// <param name="Id">The outbox message id.</param>
/// <param name="Name">The outbox message name.</param>
/// <param name="Content">The outbox message content.</param>
/// <param name="CreatedOnUtc">The outbox message created date time in UTC.</param>
/// <param name="ProcessedOnUtc">The outbox message processed date time in UTC></param>
/// <param name="Error">The outbox message error.</param>
internal sealed record OutboxMessage(
    Guid Id,
    string Name,
    string Content,
    DateTime CreatedOnUtc,
    DateTime? ProcessedOnUtc = null,
    string? Error = null);
