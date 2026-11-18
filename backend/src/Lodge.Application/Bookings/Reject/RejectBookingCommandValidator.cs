using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Bookings.Reject;

/// <summary>
/// Represents the <see cref="RejectBookingCommand"/> validator.
/// </summary>
internal sealed class RejectBookingCommandValidator : AbstractValidator<RejectBookingCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RejectBookingCommandValidator"/> class.
    /// </summary>
    public RejectBookingCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.RejectBooking.UserIdIsRequired);

        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithError(ValidationErrors.RejectBooking.BookingIdIsRequired);
    }
}
