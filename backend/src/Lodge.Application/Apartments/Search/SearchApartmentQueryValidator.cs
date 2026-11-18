using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Apartments.Search;

internal sealed class SearchApartmentQueryValidator : AbstractValidator<SearchApartmentQuery>
{
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
    }
}
