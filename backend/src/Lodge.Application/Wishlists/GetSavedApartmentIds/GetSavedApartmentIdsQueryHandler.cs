using System.Data;
using Dapper;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Wishlists.GetSavedApartmentIds;

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
