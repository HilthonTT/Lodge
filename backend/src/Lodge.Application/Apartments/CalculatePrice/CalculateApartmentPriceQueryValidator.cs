using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Apartments;

namespace Lodge.Application.Apartments.CalculatePrice;

internal sealed class CalculateApartmentPriceQueryValidator : AbstractValidator<CalculateApartmentPriceQuery>
{
    public CalculateApartmentPriceQueryValidator()
    {
        RuleFor(x => x.StartDate)
            .LessThan(c => c.EndDate)
            .WithError(ValidationErrors.CalculatePrice.StartDateMustBeLessThanEndDate);

        RuleFor(x => x)
            .Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber <= Apartment.MaxNightsLimit)
            .WithError(ValidationErrors.CalculatePrice.TooManyNights);
    }
}
