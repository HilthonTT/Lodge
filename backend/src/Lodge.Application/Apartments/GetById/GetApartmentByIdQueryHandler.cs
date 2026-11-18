using System.Data;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Apartments.GetById;

internal sealed class GetApartmentByIdQueryHandler(
    IDbConnectionFactory factory,
    IAssetUrlResolver assetUrlResolver)
    : IQueryHandler<GetApartmentByIdQuery, ApartmentResponse>
{
    public async Task<Result<ApartmentResponse>> Handle(GetApartmentByIdQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        ApartmentResponse? apartment = await ApartmentQueries.GetByIdAsync(connection, request.ApartmentId);

        if (apartment is null)
        {
            return Result.Failure<ApartmentResponse>(ApartmentErrors.NotFound(request.ApartmentId));
        }

        apartment.ImageUrl = assetUrlResolver.Resolve(apartment.ImageUrl);

        return apartment;
    }
}
