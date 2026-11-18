using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;

namespace Lodge.Application.Apartments.GetPriceRange;

/// <summary>
/// Gets the lowest and highest base nightly price of the listed apartments (for the price filter slider), converted
/// to the base currency (USD) with the static rates of <c>Currencies:Rates</c>.
/// </summary>
public sealed record GetPriceRangeQuery : IQuery<PriceRangeResponse>;
