using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.Complete;

internal sealed class CompleteSimulatedExperiencePaymentCommandValidator
    : AbstractValidator<CompleteSimulatedExperiencePaymentCommand>
{
    public CompleteSimulatedExperiencePaymentCommandValidator()
    {
        RuleFor(x => x.ExperienceBookingId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceBookingIdIsRequired);
    }
}
