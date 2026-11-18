using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Bookings.ChangeDates;

internal sealed class ChangeBookingDatesCommandValidator : AbstractValidator<ChangeBookingDatesCommand>
{
    public ChangeBookingDatesCommandValidator() =>
        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate)
            .WithError(ValidationErrors.ChangeBookingDates.StartDateMustBeLessThanEndDate);
}
