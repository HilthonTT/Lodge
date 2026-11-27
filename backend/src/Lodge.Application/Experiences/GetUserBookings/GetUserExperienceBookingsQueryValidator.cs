using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.GetUserBookings;

internal sealed class GetUserExperienceBookingsQueryValidator : AbstractValidator<GetUserExperienceBookingsQuery>
{
    public GetUserExperienceBookingsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.UserIdIsRequired);
    }
}
