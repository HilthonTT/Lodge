using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.GetSessions;

internal sealed class GetExperienceSessionsQueryValidator : AbstractValidator<GetExperienceSessionsQuery>
{
    public GetExperienceSessionsQueryValidator()
    {
        RuleFor(x => x.ExperienceId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceIdIsRequired);
    }
}
