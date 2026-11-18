using System.Buffers.Binary;
using Lodge.Application.Abstractions.Idempotency;
using Lodge.Domain.Core.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Idempotency;

/// <summary>
/// Stores idempotency keys and the results of their commands in the <c>idempotent_requests</c> table.
/// <para>
/// A transaction-scoped advisory lock on the key serializes requests with the same key: the first one holds it until
/// its transaction commits or rolls back, and a concurrent duplicate that cannot take it is reported as in progress.
/// Once the lock is taken, the committed row (if any) tells whether the key was already used. The row is inserted
/// only when the command succeeds, in the command's transaction, so a failed command does not consume the key.
/// </para>
/// </summary>
internal sealed class IdempotencyService(LodgeDbContext context, IDateTimeProvider dateTimeProvider) : IIdempotencyService
{
    public async Task<IdempotencyCheck> BeginAsync(
        Guid requestId,
        string name,
        CancellationToken cancellationToken = default)
    {
        long lockKey = LockKey(requestId);

        bool locked = await context.Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({lockKey}) AS \"Value\"")
            .SingleAsync(cancellationToken);

        if (!locked)
        {
            return IdempotencyCheck.InProgress;
        }

        IdempotentRequest? existing = await context.IdempotentRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(request => request.Id == requestId, cancellationToken);

        if (existing is null)
        {
            return IdempotencyCheck.New;
        }

        return existing.Name == name
            ? new IdempotencyCheck(IdempotencyStatus.Completed, existing.Response)
            : IdempotencyCheck.KeyReused;
    }

    public void Complete(Guid requestId, string name, string response) =>
        context.IdempotentRequests.Add(new IdempotentRequest(requestId, name, dateTimeProvider.UtcNow, response));

    /// <summary>
    /// The 64-bit advisory lock key of an idempotency key (the two halves of the GUID combined).
    /// </summary>
    private static long LockKey(Guid requestId)
    {
        Span<byte> bytes = stackalloc byte[16];
        requestId.TryWriteBytes(bytes);

        return BinaryPrimitives.ReadInt64LittleEndian(bytes) ^ BinaryPrimitives.ReadInt64LittleEndian(bytes[8..]);
    }
}
