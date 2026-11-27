using FluentValidation;

namespace Lodge.Application.Hosting.Listings.Update;

internal sealed class UpdateListingCommandValidator : AbstractValidator<UpdateListingCommand>
{
    public UpdateListingCommandValidator() =>
        RuleFor(x => x.Listing).NotNull().SetValidator(new ListingRequestValidator());
}
