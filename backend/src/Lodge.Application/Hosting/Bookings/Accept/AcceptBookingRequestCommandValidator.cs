using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Hosting.Bookings.Accept;

internal sealed class AcceptBookingRequestCommandValidator : AbstractValidator<AcceptBookingRequestCommand>
{
    public AcceptBookingRequestCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithError(ValidationErrors.Hosting.BookingIdIsRequired);
    }
}
