using Lodge.Application.Abstractions.Caching;
using Lodge.Contracts.Apartments;

namespace Lodge.Application.Apartments.GetById;

/// <summary>
/// Represents the query for fetching an apartment by its identifier.
/// </summary>
/// <param name="ApartmentId">The apartment identifier.</param>
public sealed record GetApartmentByIdQuery(Guid ApartmentId) : ICachedQuery<ApartmentResponse>
{
    public string CacheKey => CacheKeys.Apartments.GetById(ApartmentId);

    public TimeSpan? Expiration => TimeSpan.FromMinutes(20);
}
