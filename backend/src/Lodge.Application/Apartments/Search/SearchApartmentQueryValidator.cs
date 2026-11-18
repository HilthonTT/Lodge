using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Apartments;

namespace Lodge.Application.Apartments.Search;

internal sealed class SearchApartmentQueryValidator : AbstractValidator<SearchApartmentQuery>
{
    public const int MaxMonths = 12;

    public SearchApartmentQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();

        RuleFor(x => x)
            .Must(x => x.StartDate.HasValue == x.EndDate.HasValue)
            .WithError(ValidationErrors.SearchApartments.BothDatesRequired);

        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithError(ValidationErrors.SearchApartments.StartDateMustBeLessThanEndDate);

        RuleFor(x => x.Amenities)
            .Must(names => names.All(name => EnumNames.TryParse<Amenity>(name, out _)))
            .WithError(ValidationErrors.SearchApartments.InvalidAmenity);

        RuleFor(x => x.PropertyTypes)
            .Must(names => names.All(name => EnumNames.TryParse<PropertyType>(name, out _)))
            .WithError(ValidationErrors.SearchApartments.InvalidPropertyType);

        RuleFor(x => x)
            .Must(x => x.GuestCount is null or >= 1 &&
                       x.Bedrooms is null or >= 0 &&
                       x.Beds is null or >= 0 &&
                       x.Bathrooms is null or >= 0 &&
                       x.MinPrice is null or >= 0 &&
                       x.MaxPrice is null or >= 0)
            .WithError(ValidationErrors.SearchApartments.NegativeFilter);

        RuleFor(x => x)
            .Must(x => x.MinPrice is null || x.MaxPrice is null || x.MinPrice <= x.MaxPrice)
            .WithError(ValidationErrors.SearchApartments.InvalidPriceRange);

        RuleFor(x => x)
            .Must(x => x.MinLatitude is null or (>= -90 and <= 90) &&
                       x.MaxLatitude is null or (>= -90 and <= 90) &&
                       x.MinLongitude is null or (>= -180 and <= 180) &&
                       x.MaxLongitude is null or (>= -180 and <= 180) &&
                       (x.MinLatitude is null || x.MaxLatitude is null || x.MinLatitude <= x.MaxLatitude))
            .WithError(ValidationErrors.SearchApartments.InvalidBounds);

        RuleFor(x => x.Flexible)
            .Must(flexible => FlexibleStay.TryParseLength(flexible, out _))
            .When(x => x.Flexible is not null)
            .WithError(ValidationErrors.SearchApartments.InvalidFlexible);

        RuleFor(x => x)
            .Must(x => x.Flexible is null || (x.StartDate is null && x.EndDate is null))
            .WithError(ValidationErrors.SearchApartments.FlexibleWithExactDates);

        RuleFor(x => x.Months)
            .Must(months => months.Count <= MaxMonths && months.All(month => YearMonth.TryParse(month, out _)))
            .When(x => x.Flexible is not null)
            .WithError(ValidationErrors.SearchApartments.InvalidMonths);

        RuleFor(x => x.Months)
            .Empty()
            .When(x => x.Flexible is null)
            .WithError(ValidationErrors.SearchApartments.MonthsRequireFlexible);
    }
}
