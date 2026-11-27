using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.CreateSessions;

internal sealed class CreateExperienceSessionsCommandValidator : AbstractValidator<CreateExperienceSessionsCommand>
{
    public CreateExperienceSessionsCommandValidator()
    {
        RuleFor(x => x.ExperienceId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceIdIsRequired);

        RuleFor(x => x.Sessions)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.SessionsAreRequired);
    }
}
