using FluentValidation;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.GetHostExperiences;

internal sealed class GetHostExperiencesQueryValidator : AbstractValidator<GetHostExperiencesQuery>
{
    public GetHostExperiencesQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
