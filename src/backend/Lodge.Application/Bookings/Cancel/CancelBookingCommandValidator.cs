using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Bookings.Cancel;

/// <summary>
/// Represents the <see cref="CancelBookingCommand"/> validator.
/// </summary>
internal sealed class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelBookingCommandValidator"/> class.
    /// </summary>
    public CancelBookingCommandValidator()
    {
        RuleFor(x => x.UserId)
          .NotEmpty()
          .WithError(ValidationErrors.CancelBooking.UserIdIsRequired);

        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithError(ValidationErrors.CancelBooking.BookingIdIsRequired);
    }
}
