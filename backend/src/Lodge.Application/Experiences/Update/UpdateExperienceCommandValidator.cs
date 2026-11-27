using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.Update;

internal sealed class UpdateExperienceCommandValidator : AbstractValidator<UpdateExperienceCommand>
{
    public UpdateExperienceCommandValidator()
    {
        RuleFor(x => x.ExperienceId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceIdIsRequired);

        RuleFor(x => x.Experience)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceIsRequired);
    }
}
