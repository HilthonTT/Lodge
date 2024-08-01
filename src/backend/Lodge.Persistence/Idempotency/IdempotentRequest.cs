namespace Lodge.Persistence.Idempotency;

/// <summary>
/// Represents the idempotent request.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name.</param>
/// <param name="CreatedOnUtc">The created date in UTC format.</param>
public sealed record IdempotentRequest(Guid Id, string Name, DateTime CreatedOnUtc);
