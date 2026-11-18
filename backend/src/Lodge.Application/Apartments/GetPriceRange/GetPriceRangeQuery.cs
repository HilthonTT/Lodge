using Dapper;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Payments;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Apartments.GetPriceRange;

/// <summary>
/// Gets the lowest and highest base nightly price of the listed apartments (for the price filter slider), converted
/// to the base currency (USD) with the static rates of <c>Currencies:Rates</c>.
/// </summary>
public sealed record GetPriceRangeQuery : IQuery<PriceRangeResponse>;

internal sealed class GetPriceRangeQueryHandler(IDbConnectionFactory factory, CurrencyConverter currencyConverter)
    : IQueryHandler<GetPriceRangeQuery, PriceRangeResponse>
{
    public async Task<Result<PriceRangeResponse>> Handle(GetPriceRangeQuery request, CancellationToken cancellationToken)
    {
        using System.Data.IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        string price = currencyConverter.ToBaseSql("a.price_amount", "a.price_currency");

        PriceRangeRow row = await connection.QuerySingleAsync<PriceRangeRow>(
            $"""
            SELECT
                COALESCE(MIN({price}), 0) AS Min,
                COALESCE(MAX({price}), 0) AS Max
            FROM apartments AS a
            WHERE {ApartmentReader.ListedCondition}
            """);

        // Rounded outwards to whole units, so the slider bounds include every listing.
        return new PriceRangeResponse(
            Math.Floor(row.Min),
            Math.Ceiling(row.Max),
            currencyConverter.BaseCurrency);
    }

    private sealed class PriceRangeRow
    {
        public decimal Min { get; init; }

        public decimal Max { get; init; }
    }
}
