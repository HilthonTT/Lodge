using System.Data;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Apartments;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Listings.GetById;

internal sealed class GetHostListingQueryHandler(
    IHostAccess hostAccess,
    IDbConnectionFactory factory,
    ApartmentReader apartmentReader) : IQueryHandler<GetHostListingQuery, HostListingResponse>
{
    public async Task<Result<HostListingResponse>> Handle(GetHostListingQuery request, CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<HostListingResponse>(caller.Error);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        HostListingResponse? listing = await apartmentReader.GetHostListingAsync(connection, request.ApartmentId);

        if (listing is null)
        {
            return Result.Failure<HostListingResponse>(ApartmentErrors.NotFound(request.ApartmentId));
        }

        Result access = caller.Value.EnsureCanManage(listing.Host.Id);

        return access.IsFailure ? Result.Failure<HostListingResponse>(access.Error) : listing;
    }
}
