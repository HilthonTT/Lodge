namespace Lodge.Persistence.Idempotency;

/// <summary>
/// Represents a processed idempotent request.
/// </summary>
/// <param name="id">The idempotency key.</param>
/// <param name="name">The request type name.</param>
/// <param name="createdOnUtc">The created date in UTC format.</param>
/// <param name="response">
/// The serialized (JSON) result of the request, replayed for retries with the same key. Null for keys recorded before
/// results were stored.
/// </param>
public sealed class IdempotentRequest(Guid id, string name, DateTime createdOnUtc, string? response)
{
    public Guid Id { get; private set; } = id;

    public string Name { get; private set; } = name;

    public DateTime CreatedOnUtc { get; private set; } = createdOnUtc;

    public string? Response { get; private set; } = response;
}
