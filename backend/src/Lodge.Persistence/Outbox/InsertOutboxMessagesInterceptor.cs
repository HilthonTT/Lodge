using Lodge.Domain.Core.Events;
using Lodge.Domain.Core.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;

namespace Lodge.Persistence.Outbox;

/// <summary>
/// Represents the <see cref="OutboxMessage"/> interceptor.
/// </summary>
internal sealed class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
{

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            InsertOutboxMessages(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Inserts the outbox message based on the entities' domain events.
    /// </summary>
    /// <param name="context">The database context.</param>
    private static void InsertOutboxMessages(DbContext context)
    {
        DateTime utcNow = DateTime.UtcNow;

        var outboxMessages = context
            .ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                List<IDomainEvent> domainEvents = entity.DomainEvents;

                entity.ClearDomainEvents();

                return domainEvents;
            })
            .Select(domainEvent => new OutboxMessage(
                Guid.NewGuid(),
                domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
                JsonConvert.SerializeObject(domainEvent, OutboxSerializer.Settings),
                utcNow))
            .ToList();

        context.Set<OutboxMessage>().AddRange(outboxMessages);
    }
}
