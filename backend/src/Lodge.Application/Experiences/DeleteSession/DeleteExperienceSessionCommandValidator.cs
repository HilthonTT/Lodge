using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.DeleteSession;

internal sealed class DeleteExperienceSessionCommandValidator : AbstractValidator<DeleteExperienceSessionCommand>
{
    public DeleteExperienceSessionCommandValidator()
    {
        RuleFor(x => x.ExperienceId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceIdIsRequired);

        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.SessionIdIsRequired);
    }
}
