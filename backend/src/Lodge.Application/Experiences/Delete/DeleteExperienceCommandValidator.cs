using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.Delete;

internal sealed class DeleteExperienceCommandValidator : AbstractValidator<DeleteExperienceCommand>
{
    public DeleteExperienceCommandValidator()
    {
        RuleFor(x => x.ExperienceId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceIdIsRequired);
    }
}
