using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.Create;

internal sealed class CreateExperienceCommandValidator : AbstractValidator<CreateExperienceCommand>
{
    public CreateExperienceCommandValidator()
    {
        RuleFor(x => x.Experience)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceIsRequired);
    }
}
