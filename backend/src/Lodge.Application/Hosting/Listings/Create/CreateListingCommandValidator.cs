using FluentValidation;

namespace Lodge.Application.Hosting.Listings.Create;

internal sealed class CreateListingCommandValidator : AbstractValidator<CreateListingCommand>
{
    public CreateListingCommandValidator() =>
        RuleFor(x => x.Listing).NotNull().SetValidator(new ListingRequestValidator());
}
