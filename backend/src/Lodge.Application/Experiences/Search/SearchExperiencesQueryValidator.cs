using FluentValidation;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences.Search;

internal sealed class SearchExperiencesQueryValidator : AbstractValidator<SearchExperiencesQuery>
{
    public SearchExperiencesQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();

        RuleFor(x => x.Category)
            .Must(category => string.IsNullOrWhiteSpace(category) || EnumNames.TryParse<ExperienceCategory>(category, out _))
            .WithError(ExperienceErrors.InvalidCategory);

        RuleFor(x => x.Guests)
            .InclusiveBetween(1, Experience.MaxGuestsLimit)
            .When(x => x.Guests is not null)
            .WithError(ExperienceBookingErrors.InvalidGuestCount);

        RuleFor(x => x)
            .Must(x => x.MinPrice is null or >= 0 && x.MaxPrice is null or >= 0 &&
                       (x.MinPrice is null || x.MaxPrice is null || x.MinPrice <= x.MaxPrice))
            .WithError(Core.Errors.ValidationErrors.SearchApartments.InvalidPriceRange);
    }
}
