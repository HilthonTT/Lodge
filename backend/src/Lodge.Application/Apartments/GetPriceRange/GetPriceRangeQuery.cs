using Dapper;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Apartments.GetPriceRange;

/// <summary>
/// Gets the lowest and highest base nightly price of the listed apartments (for the price filter slider).
/// </summary>
public sealed record GetPriceRangeQuery : IQuery<PriceRangeResponse>;

internal sealed class GetPriceRangeQueryHandler(IDbConnectionFactory factory)
    : IQueryHandler<GetPriceRangeQuery, PriceRangeResponse>
{
    private const string DefaultCurrency = "USD";

    public async Task<Result<PriceRangeResponse>> Handle(GetPriceRangeQuery request, CancellationToken cancellationToken)
    {
        using System.Data.IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        PriceRangeRow row = await connection.QuerySingleAsync<PriceRangeRow>(
            $"""
            SELECT
                COALESCE(MIN(a.price_amount), 0) AS Min,
                COALESCE(MAX(a.price_amount), 0) AS Max,
                MIN(a.price_currency) AS Currency
            FROM apartments AS a
            WHERE {ApartmentReader.ListedCondition}
            """);

        return new PriceRangeResponse(row.Min, row.Max, row.Currency ?? DefaultCurrency);
    }

    private sealed class PriceRangeRow
    {
        public decimal Min { get; init; }

        public decimal Max { get; init; }

        public string? Currency { get; init; }
    }
}
