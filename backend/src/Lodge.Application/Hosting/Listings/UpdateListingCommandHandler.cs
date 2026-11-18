using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Listings;

internal sealed class UpdateListingCommandHandler(
    IHostAccess hostAccess,
    IApartmentRepository apartmentRepository,
    IBlobService blobService,
    ICacheInvalidator cacheInvalidator) : ICommandHandler<UpdateListingCommand>
{
    public async Task<Result> Handle(UpdateListingCommand request, CancellationToken cancellationToken)
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

        Result<ListingDetails> details = ListingMapper.ToDetails(request.Listing);

        if (details.IsFailure)
        {
            return details;
        }

        Result result = apartment.Update(details.Value);

        if (result.IsFailure)
        {
            return result;
        }

        Result<List<ApartmentImage>> images = await ListingMapper.ResolveImagesAsync(
            blobService,
            apartment,
            request.Listing.ImageIds,
            caller.Value.UserId,
            cancellationToken);

        if (images.IsFailure)
        {
            return images;
        }

        result = apartment.SetImages(images.Value);

        if (result.IsFailure)
        {
            return result;
        }

        if (request.Listing.IsListed is bool isListed)
        {
            apartment.SetListed(isListed);
        }

        cacheInvalidator.InvalidateOnCommit(CacheKeys.Apartments.GetById(apartment.Id));

        return Result.Success();
    }
}
