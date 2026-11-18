using System.Data;
using Dapper;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Apartments;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Wishlists;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Wishlists;

namespace Lodge.Application.Wishlists;

/// <summary>
/// Gets the current user's wishlists, most recently changed first. Unlisted and deleted apartments are not counted.
/// </summary>
public sealed record GetWishlistsQuery : IQuery<List<WishlistSummaryResponse>>;

/// <summary>
/// Gets one of the current user's wishlists with its listed apartments, most recently saved first.
/// </summary>
public sealed record GetWishlistByIdQuery(Guid WishlistId) : IQuery<WishlistResponse>;

/// <summary>
/// Gets the ids of every apartment the current user saved in any wishlist (for the heart icons).
/// </summary>
public sealed record GetSavedApartmentIdsQuery : IQuery<List<Guid>>;

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

internal sealed class GetWishlistByIdQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory,
    ApartmentReader apartmentReader) : IQueryHandler<GetWishlistByIdQuery, WishlistResponse>
{
    public async Task<Result<WishlistResponse>> Handle(GetWishlistByIdQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        string? name = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT w.name FROM wishlists AS w WHERE w.id = @WishlistId AND w.user_id = @UserId",
            new { request.WishlistId, userIdentifierProvider.UserId });

        if (name is null)
        {
            return Result.Failure<WishlistResponse>(WishlistErrors.NotFound(request.WishlistId));
        }

        List<ApartmentResponse> items = await apartmentReader.QueryAsync(
            connection,
            $"""
            INNER JOIN wishlist_items AS wi ON wi.apartment_id = a.id
            WHERE wi.wishlist_id = @WishlistId AND {ApartmentReader.ListedCondition}
            ORDER BY wi.added_on_utc DESC, a.id
            """,
            new { request.WishlistId });

        return new WishlistResponse(request.WishlistId, name, items);
    }
}

internal sealed class GetSavedApartmentIdsQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory) : IQueryHandler<GetSavedApartmentIdsQuery, List<Guid>>
{
    public async Task<Result<List<Guid>>> Handle(GetSavedApartmentIdsQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        IEnumerable<Guid> ids = await connection.QueryAsync<Guid>(
            """
            SELECT DISTINCT wi.apartment_id
            FROM wishlist_items AS wi
            INNER JOIN wishlists AS w ON w.id = wi.wishlist_id
            WHERE w.user_id = @UserId
            ORDER BY wi.apartment_id
            """,
            new { userIdentifierProvider.UserId });

        return ids.ToList();
    }
}
