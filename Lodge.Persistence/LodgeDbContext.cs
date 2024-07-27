using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Apartements;
using Lodge.Domain.Bookings;
using Lodge.Domain.Reviews;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Lodge.Persistence;

internal sealed class LodgeDbContext(DbContextOptions<LodgeDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users { get; set; }

    public DbSet<Apartment> Apartments { get; set; }

    public DbSet<Booking> Bookings { get; set; }

    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LodgeDbContext).Assembly);
    }

    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        IDbContextTransaction transaction = await Database.BeginTransactionAsync(cancellationToken);

        return transaction.GetDbTransaction();
    }
}
