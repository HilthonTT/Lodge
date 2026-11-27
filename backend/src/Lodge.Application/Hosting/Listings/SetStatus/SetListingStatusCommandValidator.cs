using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Hosting.Listings.SetStatus;

internal sealed class SetListingStatusCommandValidator : AbstractValidator<SetListingStatusCommand>
{
    public SetListingStatusCommandValidator()
    {
        RuleFor(x => x.ApartmentId)
            .NotEmpty()
            .WithError(ValidationErrors.Hosting.ApartmentIdIsRequired);
    }
}
