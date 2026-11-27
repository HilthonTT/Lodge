using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.Checkout;

internal sealed class ExperienceCheckoutCommandValidator : AbstractValidator<ExperienceCheckoutCommand>
{
    public ExperienceCheckoutCommandValidator()
    {
        RuleFor(x => x.ExperienceBookingId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceBookingIdIsRequired);
    }
}
