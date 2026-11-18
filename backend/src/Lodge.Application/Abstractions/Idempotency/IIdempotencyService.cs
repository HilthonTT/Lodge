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

/// <summary>
/// The state of an idempotency key.
/// </summary>
public enum IdempotencyStatus
{
    /// <summary>The key is new: run the command.</summary>
    New,

    /// <summary>The key was used by a successful request of the same type.</summary>
    Completed,

    /// <summary>The key was used by a request of another type.</summary>
    KeyReused,

    /// <summary>Another request with the same key is still being processed.</summary>
    InProgress,
}

/// <summary>
/// The result of <see cref="IIdempotencyService.BeginAsync"/>.
/// </summary>
/// <param name="Status">The state of the key.</param>
/// <param name="Response">
/// The serialized result of the first request (<see cref="IdempotencyStatus.Completed"/>); null for keys recorded
/// before results were stored.
/// </param>
public sealed record IdempotencyCheck(IdempotencyStatus Status, string? Response = null)
{
    public static readonly IdempotencyCheck New = new(IdempotencyStatus.New);

    public static readonly IdempotencyCheck KeyReused = new(IdempotencyStatus.KeyReused);

    public static readonly IdempotencyCheck InProgress = new(IdempotencyStatus.InProgress);
}
