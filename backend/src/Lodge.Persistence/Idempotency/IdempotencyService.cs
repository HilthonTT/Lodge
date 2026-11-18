using Lodge.Application.Abstractions.Idempotency;
using Lodge.Domain.Core.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Idempotency;

/// <summary>
/// Represents the idempotency service.
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="dateTimeProvider">The date time provider.</param>
internal sealed class IdempotencyService(
    LodgeDbContext context, 
    IDateTimeProvider dateTimeProvider) : IIdempotencyService
{
    /// <inheritdoc />
    public Task CreateRequestAsync(Guid requestId, string name, CancellationToken cancellationToken = default)
    {
        var idempotentRequest = new IdempotentRequest(requestId, name, dateTimeProvider.UtcNow);

        context.IdempotentRequests.Add(idempotentRequest);

        return context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> RequestExistsAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return context.IdempotentRequests.AnyAsync(r => r.Id == requestId, cancellationToken);
    }
}
