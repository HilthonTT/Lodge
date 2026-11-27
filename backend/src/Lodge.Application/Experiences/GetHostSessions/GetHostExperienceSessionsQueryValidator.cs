using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.GetHostSessions;

internal sealed class GetHostExperienceSessionsQueryValidator : AbstractValidator<GetHostExperienceSessionsQuery>
{
    public GetHostExperienceSessionsQueryValidator()
    {
        RuleFor(x => x.ExperienceId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceIdIsRequired);
    }
}
