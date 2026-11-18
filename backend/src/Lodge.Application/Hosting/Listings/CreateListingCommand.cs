using FluentValidation;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Contracts.Hosting;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Listings;

/// <summary>
/// Creates a listing owned by the current user (who needs a verified e-mail address).
/// </summary>
public sealed record CreateListingCommand(ListingRequest Listing) : ICommand<Guid>;

internal sealed class CreateListingCommandValidator : AbstractValidator<CreateListingCommand>
{
    public CreateListingCommandValidator() =>
        RuleFor(x => x.Listing).NotNull().SetValidator(new ListingRequestValidator());
}

internal sealed class CreateListingCommandHandler(
    IHostAccess hostAccess,
    IApartmentRepository apartmentRepository,
    IBlobService blobService) : ICommandHandler<CreateListingCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateListingCommand request, CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<Guid>(caller.Error);
        }

        Result<ListingDetails> details = ListingMapper.ToDetails(request.Listing);

        if (details.IsFailure)
        {
            return Result.Failure<Guid>(details.Error);
        }

        Result<List<ApartmentImage>> images = await ListingMapper.ResolveImagesAsync(
            blobService,
            apartment: null,
            request.Listing.ImageIds,
            caller.Value.UserId,
            cancellationToken);

        if (images.IsFailure)
        {
            return Result.Failure<Guid>(images.Error);
        }

        Result<Apartment> apartment = Apartment.Create(
            caller.Value.UserId,
            details.Value,
            images.Value,
            request.Listing.IsListed ?? true);

        if (apartment.IsFailure)
        {
            return Result.Failure<Guid>(apartment.Error);
        }

        apartmentRepository.Insert(apartment.Value);

        return apartment.Value.Id;
    }
}
