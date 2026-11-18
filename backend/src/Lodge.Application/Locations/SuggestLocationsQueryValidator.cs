using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Locations;

internal sealed class SuggestLocationsQueryValidator : AbstractValidator<SuggestLocationsQuery>
{
    public SuggestLocationsQueryValidator()
    {
        RuleFor(x => x.Query)
            .Must(query => query?.Trim().Length is >= SuggestLocationsQuery.MinLength and <= SuggestLocationsQuery.MaxLength)
            .WithError(ValidationErrors.Locations.InvalidQuery);
    }
}
