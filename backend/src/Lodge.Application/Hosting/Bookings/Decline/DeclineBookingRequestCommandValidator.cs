using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Hosting.Bookings.Decline;

internal sealed class DeclineBookingRequestCommandValidator : AbstractValidator<DeclineBookingRequestCommand>
{
    public DeclineBookingRequestCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithError(ValidationErrors.Hosting.BookingIdIsRequired);
    }
}
