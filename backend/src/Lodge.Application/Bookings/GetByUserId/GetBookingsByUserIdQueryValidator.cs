using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Bookings.GetByUserId;

internal sealed class GetBookingsByUserIdQueryValidator : AbstractValidator<GetBookingsByUserIdQuery>
{
    public GetBookingsByUserIdQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.Bookings.UserIdIsRequired);
    }
}
