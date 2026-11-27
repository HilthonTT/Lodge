using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Apartments.GetDisabledDates;

internal sealed class GetApartmentDisabledDatesQueryValidator : AbstractValidator<GetApartmentDisabledDatesQuery>
{
    public GetApartmentDisabledDatesQueryValidator()
    {
        RuleFor(x => x.ApartmentId)
            .NotEmpty()
            .WithError(ValidationErrors.Apartments.ApartmentIdIsRequired);
    }
}
