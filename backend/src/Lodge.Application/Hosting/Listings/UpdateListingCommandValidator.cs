using FluentValidation;

namespace Lodge.Application.Hosting.Listings;

internal sealed class UpdateListingCommandValidator : AbstractValidator<UpdateListingCommand>
{
    public UpdateListingCommandValidator() =>
        RuleFor(x => x.Listing).NotNull().SetValidator(new ListingRequestValidator());
}
