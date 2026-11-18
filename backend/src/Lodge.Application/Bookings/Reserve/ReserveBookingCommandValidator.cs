using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Bookings.Reserve;

/// <summary>
/// Represents the <see cref="ReserveBookingCommand"/> validator.
/// </summary>
internal sealed class ReserveBookingCommandValidator : AbstractValidator<ReserveBookingCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReserveBookingCommandValidator"/> class.
    /// </summary>
    public ReserveBookingCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.ReserveBooking.UserIdIsRequired);

        RuleFor(x => x.ApartmentId)
            .NotEmpty()
            .WithError(ValidationErrors.ReserveBooking.ApartmentIdIsRequired);

        RuleFor(x => x.StartDate)
            .LessThan(c => c.EndDate)
            .WithError(ValidationErrors.ReserveBooking.StartDateMustBeLessThanEndDate);
    }
}
