using Lodge.Domain.Core.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Application.Abstractions.Data;

/// <summary>
/// Represents the application database.
/// </summary>
public interface IDbContext
{
    /// <summary>
    /// Gets the database set for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>The database set for the specified entity type.</returns>
    DbSet<TEntity> Set<TEntity>()
        where TEntity : Entity;
}
