using FluentValidation;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences;

internal sealed class GetExperienceReviewsQueryValidator : AbstractValidator<GetExperienceReviewsQuery>
{
    public GetExperienceReviewsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
