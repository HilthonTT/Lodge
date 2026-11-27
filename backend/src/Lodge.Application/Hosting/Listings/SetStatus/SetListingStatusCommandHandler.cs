using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Listings.SetStatus;

internal sealed class SetListingStatusCommandHandler(
    IHostAccess hostAccess,
    IApartmentRepository apartmentRepository,
    ICacheInvalidator cacheInvalidator) : ICommandHandler<SetListingStatusCommand>
{
    public async Task<Result> Handle(SetListingStatusCommand request, CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return caller;
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failure(ApartmentErrors.NotFound(request.ApartmentId));
        }

        Result access = caller.Value.EnsureCanManage(apartment);

        if (access.IsFailure)
        {
            return access;
        }

        apartment.SetListed(request.IsListed);

        cacheInvalidator.InvalidateOnCommit(CacheKeys.Apartments.GetById(apartment.Id));

        return Result.Success();
    }
}
