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

namespace Lodge.Application.Wishlists.GetById;

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
