using Lodge.Domain.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lodge.Persistence.Interceptors;

internal sealed class SoftDeleteEntitiesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            DeleteSoftDeletableEntites(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void DeleteSoftDeletableEntites(DbContext context)
    {
        DateTime utcNow = DateTime.UtcNow;

        IEnumerable<EntityEntry<ISoftDeletableEntity>> entries =
            context
                .ChangeTracker
                .Entries<ISoftDeletableEntity>()
                .Where(e => e.State == EntityState.Deleted);

        foreach (EntityEntry<ISoftDeletableEntity> softDeletable in entries)
        {
            softDeletable.State = EntityState.Modified;

            softDeletable.Property(a => a.Deleted).CurrentValue = true;

            softDeletable.Property(a => a.DeletedOnUtc).CurrentValue = utcNow;
        }
    }
}
