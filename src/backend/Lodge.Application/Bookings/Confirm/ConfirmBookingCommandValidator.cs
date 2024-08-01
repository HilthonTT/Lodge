using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Bookings.Confirm;

/// <summary>
/// Represents the <see cref="ConfirmBookingCommand"/> validator.
/// </summary>
internal sealed class ConfirmBookingCommandValidator : AbstractValidator<ConfirmBookingCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmBookingCommandValidator"/> class.
    /// </summary>
    public ConfirmBookingCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.ConfirmBooking.UserIdIsRequired);

        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithError(ValidationErrors.ConfirmBooking.BookingIdIsRequired);
    }
}
