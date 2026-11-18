using Lodge.Application.Abstractions.Data;
using Lodge.Application.Core.Exceptions;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;
using Lodge.Domain.Users;
using Lodge.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lodge.Persistence;

/// <summary>
/// The EF Core database context; also the unit of work.
/// </summary>
public sealed class LodgeDbContext(DbContextOptions<LodgeDbContext> options)
    : DbContext(options), IUnitOfWork, IDbContext
{
    public DbSet<User> Users { get; set; } = null!;

    public DbSet<Apartment> Apartments { get; set; } = null!;

    public DbSet<Booking> Bookings { get; set; } = null!;

    public DbSet<Review> Reviews { get; set; } = null!;

    public DbSet<IdempotentRequest> IdempotentRequests { get; set; } = null!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyException("A concurrency conflict occurred while saving changes.", exception);
        }
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(cancellationToken);

    public void DiscardChanges() => ChangeTracker.Clear();

    public new DbSet<TEntity> Set<TEntity>()
        where TEntity : Entity
        => base.Set<TEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LodgeDbContext).Assembly);
}
