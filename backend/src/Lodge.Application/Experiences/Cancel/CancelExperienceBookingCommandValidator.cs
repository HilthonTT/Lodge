using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.Cancel;

internal sealed class CancelExperienceBookingCommandValidator : AbstractValidator<CancelExperienceBookingCommand>
{
    public CancelExperienceBookingCommandValidator()
    {
        RuleFor(x => x.ExperienceBookingId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceBookingIdIsRequired);
    }
}
