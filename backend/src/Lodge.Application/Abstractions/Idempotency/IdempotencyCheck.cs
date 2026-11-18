namespace Lodge.Application.Abstractions.Idempotency;

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
