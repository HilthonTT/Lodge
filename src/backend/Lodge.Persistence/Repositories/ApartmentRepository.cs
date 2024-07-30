using Lodge.Domain.Apartements;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the apartment repository.
/// </summary>
/// <param name="context">The database context.</param>
internal sealed class ApartmentRepository(LodgeDbContext context) : IApartmentRepository
{
    /// <inheritdoc />
    public Task<Apartment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Apartments.FirstOrDefaultAsync(apartment => apartment.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public void Insert(Apartment apartment)
    {
       context.Apartments.Add(apartment);
    }

    /// <inheritdoc />
    public void Remove(Apartment apartment)
    {
        context.Apartments.Remove(apartment);
    }
}
