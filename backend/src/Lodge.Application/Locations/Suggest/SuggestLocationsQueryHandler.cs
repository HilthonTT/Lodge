using System.Data;
using Dapper;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Apartments;
using Lodge.Application.Core.Extensions;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Locations.Suggest;

internal sealed class SuggestLocationsQueryHandler(IDbConnectionFactory factory)
    : IQueryHandler<SuggestLocationsQuery, List<LocationSuggestionResponse>>
{
    public async Task<Result<List<LocationSuggestionResponse>>> Handle(
        SuggestLocationsQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        IEnumerable<SuggestionRow> rows = await connection.QueryAsync<SuggestionRow>(
            $"""
            SELECT
                a.address_city AS City,
                a.address_country AS Country,
                AVG(a.latitude) AS Latitude,
                AVG(a.longitude) AS Longitude,
                COUNT(*)::int AS ListingCount
            FROM apartments AS a
            WHERE {ApartmentReader.ListedCondition}
              AND (unaccent(a.address_city) ILIKE unaccent(@Pattern)
                OR unaccent(a.address_state) ILIKE unaccent(@Pattern)
                OR unaccent(a.address_country) ILIKE unaccent(@Pattern))
            GROUP BY a.address_city, a.address_country
            ORDER BY ListingCount DESC, a.address_city, a.address_country
            LIMIT {SuggestLocationsQuery.MaxResults}
            """,
            new { Pattern = SqlPatterns.Contains(request.Query!) });

        return rows
            .Select(row => new LocationSuggestionResponse(
                $"{row.City}, {row.Country}",
                row.City,
                row.Country,
                Math.Round(row.Latitude, 6),
                Math.Round(row.Longitude, 6),
                row.ListingCount))
            .ToList();
    }

    private sealed class SuggestionRow
    {
        public string City { get; init; } = string.Empty;

        public string Country { get; init; } = string.Empty;

        public double Latitude { get; init; }

        public double Longitude { get; init; }

        public int ListingCount { get; init; }
    }
}
