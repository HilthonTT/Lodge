using Lodge.Application.Abstractions.Caching;
using Lodge.Contracts.Common;

namespace Lodge.Application.Apartments.CalculatePrice;

/// <summary>
/// Calculates the price of staying in an apartment for the given period.
/// </summary>
public sealed record CalculateApartmentPriceQuery(
    Guid ApartmentId,
    DateOnly StartDate,
    DateOnly EndDate) : ICachedQuery<PriceDetailsResponse>
{
    public string CacheKey => CacheKeys.Apartments.GetPrice(ApartmentId, StartDate, EndDate);

    public TimeSpan? Expiration => TimeSpan.FromMinutes(20);
}
