using System.Data;
using Dapper;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Apartments;
using Lodge.Contracts.Wishlists;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Wishlists;

internal sealed class GetWishlistsQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory,
    ApartmentReader apartmentReader) : IQueryHandler<GetWishlistsQuery, List<WishlistSummaryResponse>>
{
    public async Task<Result<List<WishlistSummaryResponse>>> Handle(
        GetWishlistsQuery request,
        CancellationToken cancellationToken)
    {
        string sql =
            $"""
            SELECT
                w.id AS Id,
                w.name AS Name,
                w.updated_on_utc AS UpdatedOnUtc,
                COALESCE(items.item_count, 0) AS ItemCount,
                cover.id AS CoverImageId,
                cover.path AS CoverImagePath
            FROM wishlists AS w
            LEFT JOIN LATERAL (
                SELECT COUNT(*)::int AS item_count
                FROM wishlist_items AS wi
                INNER JOIN apartments AS a ON a.id = wi.apartment_id
                WHERE wi.wishlist_id = w.id AND {ApartmentReader.ListedCondition}
            ) AS items ON TRUE
            LEFT JOIN LATERAL (
                SELECT i.id, i.path
                FROM wishlist_items AS wi
                INNER JOIN apartments AS a ON a.id = wi.apartment_id
                INNER JOIN LATERAL (
                    SELECT ai.id, ai.path FROM apartment_images AS ai
                    WHERE ai.apartment_id = a.id
                    ORDER BY ai.position
                    LIMIT 1
                ) AS i ON TRUE
                WHERE wi.wishlist_id = w.id AND {ApartmentReader.ListedCondition}
                ORDER BY wi.added_on_utc DESC
                LIMIT 1
            ) AS cover ON TRUE
            WHERE w.user_id = @UserId
            ORDER BY w.updated_on_utc DESC, w.id
            """;

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        IEnumerable<WishlistRow> rows = await connection.QueryAsync<WishlistRow>(
            sql,
            new { userIdentifierProvider.UserId });

        return rows
            .Select(row => new WishlistSummaryResponse(
                row.Id,
                row.Name,
                row.ItemCount,
                row.CoverImageId is Guid imageId ? apartmentReader.ImageUrl(imageId, row.CoverImagePath) : null,
                row.UpdatedOnUtc))
            .ToList();
    }

    private sealed class WishlistRow
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public DateTime UpdatedOnUtc { get; init; }

        public int ItemCount { get; init; }

        public Guid? CoverImageId { get; init; }

        public string? CoverImagePath { get; init; }
    }
}
