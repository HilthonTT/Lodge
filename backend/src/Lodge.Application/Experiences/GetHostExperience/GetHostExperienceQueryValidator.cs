using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.GetHostExperience;

internal sealed class GetHostExperienceQueryValidator : AbstractValidator<GetHostExperienceQuery>
{
    public GetHostExperienceQueryValidator()
    {
        RuleFor(x => x.ExperienceId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceIdIsRequired);
    }
}
