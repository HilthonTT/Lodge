using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences.Reserve;

internal sealed class ReserveExperienceCommandValidator : AbstractValidator<ReserveExperienceCommand>
{
    public ReserveExperienceCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.RequestIdIsRequired);

        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.SessionIdIsRequired);

        RuleFor(x => x.GuestCount)
            .GreaterThanOrEqualTo(1)
            .WithError(ExperienceBookingErrors.InvalidGuestCount);
    }
}
