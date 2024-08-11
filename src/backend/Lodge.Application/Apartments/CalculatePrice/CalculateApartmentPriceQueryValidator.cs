using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Apartments.CalculatePrice;

/// <summary>
/// Represents the <see cref="CalculateApartmentPriceQuery"/> validator.
/// </summary>
internal sealed class CalculateApartmentPriceQueryValidator : AbstractValidator<CalculateApartmentPriceQuery>
{
    /// <summary>
    /// Initializes a new instance of <see cref="CalculateApartmentPriceQueryValidator"/> class.
    /// </summary>
    public CalculateApartmentPriceQueryValidator()
    {
        RuleFor(x => x.StartDate)
            .LessThan(c => c.EndDate)
            .WithError(ValidationErrors.CalculatePrice.StartDateMustBeLessThanEndDate);
    }
}
