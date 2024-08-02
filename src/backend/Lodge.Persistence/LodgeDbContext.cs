using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Apartements;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;
using Lodge.Domain.Users;
using Lodge.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lodge.Persistence;

public sealed class LodgeDbContext(DbContextOptions<LodgeDbContext> options)
    : DbContext(options), IUnitOfWork, IDbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<Apartment> Apartments { get; set; }

    public DbSet<Booking> Bookings { get; set; }

    public DbSet<Review> Reviews { get; set; }

    public DbSet<IdempotentRequest> IdempotentRequests { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LodgeDbContext).Assembly);
    }

    /// <inheritdoc />
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        IDbContextTransaction transaction = await Database.BeginTransactionAsync(cancellationToken);

        return transaction;
    }

    /// <inheritdoc />
    public new DbSet<TEntity> Set<TEntity>()
            where TEntity : Entity
            => base.Set<TEntity>();
}
