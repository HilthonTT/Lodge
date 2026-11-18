using System.Data;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Apartments.Get;

internal sealed class GetApartmentsQueryHandler(
    IDbConnectionFactory factory,
    IAssetUrlResolver assetUrlResolver)
    : IQueryHandler<GetApartmentsQuery, PagedList<ApartmentResponse>>
{
    public async Task<Result<PagedList<ApartmentResponse>>> Handle(
        GetApartmentsQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<ApartmentResponse> items, int totalCount) =
            await ApartmentQueries.GetPageAsync(connection, request.Page, request.PageSize);

        foreach (ApartmentResponse apartment in items)
        {
            apartment.ImageUrl = assetUrlResolver.Resolve(apartment.ImageUrl);
        }

        return new PagedList<ApartmentResponse>(items, request.Page, request.PageSize, totalCount);
    }
}
