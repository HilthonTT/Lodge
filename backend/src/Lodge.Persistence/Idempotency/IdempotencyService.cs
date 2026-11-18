using Lodge.Application.Abstractions.Idempotency;
using Lodge.Domain.Core.Primitives;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lodge.Persistence.Idempotency;

/// <summary>
/// Stores idempotency keys in the <c>idempotent_requests</c> table. The primary key guarantees uniqueness
/// even when two requests with the same key race each other.
/// </summary>
internal sealed class IdempotencyService(LodgeDbContext context, IDateTimeProvider dateTimeProvider) : IIdempotencyService
{
    public async Task<bool> TryCreateRequestAsync(Guid requestId, string name, CancellationToken cancellationToken = default)
    {
        if (await context.IdempotentRequests.AnyAsync(r => r.Id == requestId, cancellationToken))
        {
            return false;
        }

        var idempotentRequest = new IdempotentRequest(requestId, name, dateTimeProvider.UtcNow);

        context.IdempotentRequests.Add(idempotentRequest);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            context.Entry(idempotentRequest).State = EntityState.Detached;
            return false;
        }
    }
}
