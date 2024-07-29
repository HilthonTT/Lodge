using Lodge.Domain.Apartements;
using Lodge.Domain.Core.Primitives.Maybe;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the apartment repository.
/// </summary>
/// <param name="context">The database context.</param>
internal sealed class ApartmentRepository(LodgeDbContext context) : IApartmentRepository
{
    /// <inheritdoc />
    public async Task<Maybe<Apartment?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Apartments.FirstOrDefaultAsync(apartment => apartment.Id == id, cancellationToken);
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
