using Lodge.Application.Abstractions.Caching;
using Lodge.Contracts.Common;

namespace Lodge.Application.Apartments.CalculatePrice;

/// <summary>
/// Represents the query for calculating the price of an apartment.
/// </summary>
/// <param name="ApartmentId">The apartment identifier.</param>
/// <param name="StartDate">The start date.</param>
/// <param name="EndDate">The end date.</param>
public sealed record CalculateApartmentPriceQuery(
    Guid ApartmentId,
    DateOnly StartDate,
    DateOnly EndDate) : ICachedQuery<PriceDetailsResponse>
{
    public string CacheKey => CacheKeys.Apartments.GetPrice(ApartmentId);

    public TimeSpan? Expiration => TimeSpan.FromMinutes(20);
}
