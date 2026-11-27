using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.GetBooking;

internal sealed class GetExperienceBookingQueryValidator : AbstractValidator<GetExperienceBookingQuery>
{
    public GetExperienceBookingQueryValidator()
    {
        RuleFor(x => x.ExperienceBookingId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceBookingIdIsRequired);
    }
}
