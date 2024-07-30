using Lodge.Domain.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lodge.Persistence.Interceptors;

/// <summary>
/// Represents the update auditable interceptor managing the entities
/// implementing the <see cref="IAuditableEntity"/> interface.
/// </summary>
internal sealed class UpdateAuditableEntitiesInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            UpdateAuditableEntities(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Updates the entities implementing the <see cref="IAuditableEntity"/> interface.
    /// </summary>
    /// <param name="context">The database context.</param>
    private static void UpdateAuditableEntities(DbContext context)
    {
        DateTime utcNow = DateTime.UtcNow;

        IEnumerable<EntityEntry<IAuditableEntity>> entries = 
            context
                .ChangeTracker
                .Entries<IAuditableEntity>();

        foreach (EntityEntry<IAuditableEntity> auditableEntity in entries)
        {
            if (auditableEntity.State == EntityState.Added)
            {
                auditableEntity.Property(a => a.CreatedOnUtc).CurrentValue = utcNow;
            }

            if (auditableEntity.State == EntityState.Modified)
            {
                auditableEntity.Property(a => a.ModifiedOnUtc).CurrentValue = utcNow;
            }
        }
    }
}
