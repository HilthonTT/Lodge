using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Bookings.Complete;

/// <summary>
/// Represents the <see cref="CompleteBookingCommand"/> validator.
/// </summary>
internal sealed class CompleteBookingCommandValidator : AbstractValidator<CompleteBookingCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteBookingCommandValidator"/> class.
    /// </summary>
    public CompleteBookingCommandValidator()
    {
        RuleFor(x => x.UserId)
           .NotEmpty()
           .WithError(ValidationErrors.CompleteBooking.UserIdIsRequired);

        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithError(ValidationErrors.CompleteBooking.BookingIdIsRequired);
    }
}
