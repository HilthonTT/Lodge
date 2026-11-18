using Lodge.Domain.Apartments;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the apartment repository. Soft-deleted apartments are hidden by the query filter.
/// </summary>
/// <param name="context">The database context.</param>
internal sealed class ApartmentRepository(LodgeDbContext context) : IApartmentRepository
{
    /// <inheritdoc />
    public Task<Apartment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Apartments.FirstOrDefaultAsync(apartment => apartment.Id == id, cancellationToken);

    /// <inheritdoc />
    public void Insert(Apartment apartment) => context.Apartments.Add(apartment);
}
