using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Apartments.CalculatePrice;

internal sealed class CalculateApartmentPriceQueryValidator : AbstractValidator<CalculateApartmentPriceQuery>
{
    public CalculateApartmentPriceQueryValidator()
    {
        RuleFor(x => x.StartDate)
            .LessThan(c => c.EndDate)
            .WithError(ValidationErrors.CalculatePrice.StartDateMustBeLessThanEndDate);
    }
}
