using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Hosting.Listings.GetById;

internal sealed class GetHostListingQueryValidator : AbstractValidator<GetHostListingQuery>
{
    public GetHostListingQueryValidator()
    {
        RuleFor(x => x.ApartmentId)
            .NotEmpty()
            .WithError(ValidationErrors.Hosting.ApartmentIdIsRequired);
    }
}
