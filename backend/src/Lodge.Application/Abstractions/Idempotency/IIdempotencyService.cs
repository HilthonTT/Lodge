namespace Lodge.Application.Abstractions.Idempotency;

/// <summary>
/// Records processed idempotency keys.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Stores the request identifier. Returns <see langword="false"/> when the identifier was already used.
    /// </summary>
    Task<bool> TryCreateRequestAsync(Guid requestId, string name, CancellationToken cancellationToken = default);
}
