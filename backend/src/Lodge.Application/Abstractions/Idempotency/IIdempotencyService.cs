namespace Lodge.Application.Abstractions.Idempotency;

/// <summary>
/// Records processed idempotency keys together with the serialized result of the command, so that a retry with the
/// same key replays the first result. Both methods run inside the command's database transaction.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Claims the key for the current transaction and reports whether it was already used. A concurrent request with
    /// the same key that has not committed yet is reported as <see cref="IdempotencyStatus.InProgress"/>.
    /// </summary>
    /// <param name="requestId">The idempotency key.</param>
    /// <param name="name">The request type name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IdempotencyCheck> BeginAsync(Guid requestId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the key with the serialized result of the successful command. The record is saved and committed
    /// together with the command (a failed command rolls it back, so the key can be reused).
    /// </summary>
    /// <param name="requestId">The idempotency key.</param>
    /// <param name="name">The request type name.</param>
    /// <param name="response">The serialized result.</param>
    void Complete(Guid requestId, string name, string response);
}
