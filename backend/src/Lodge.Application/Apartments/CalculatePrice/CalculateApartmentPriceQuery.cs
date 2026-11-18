using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;

namespace Lodge.Application.Apartments.CalculatePrice;

/// <summary>
/// Calculates the price of staying in a listed apartment for the given period. Not cached: the price depends on the
/// host's calendar (custom nightly prices), which changes independently of the apartment.
/// </summary>
public sealed record CalculateApartmentPriceQuery(
    Guid ApartmentId,
    DateOnly StartDate,
    DateOnly EndDate) : IQuery<PriceDetailsResponse>;
