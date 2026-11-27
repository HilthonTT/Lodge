using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Hosting.Bookings.Cancel;

internal sealed class HostCancelBookingCommandValidator : AbstractValidator<HostCancelBookingCommand>
{
    public HostCancelBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithError(ValidationErrors.Hosting.BookingIdIsRequired);
    }
}
