using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.SetStatus;

internal sealed class SetExperienceStatusCommandValidator : AbstractValidator<SetExperienceStatusCommand>
{
    public SetExperienceStatusCommandValidator()
    {
        RuleFor(x => x.ExperienceId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceIdIsRequired);
    }
}
