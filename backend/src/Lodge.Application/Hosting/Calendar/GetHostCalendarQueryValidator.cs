using FluentValidation;
using Lodge.Application.Apartments.GetCalendar;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Apartments;

namespace Lodge.Application.Hosting.Calendar;

internal sealed class GetHostCalendarQueryValidator : AbstractValidator<GetHostCalendarQuery>
{
    public GetHostCalendarQueryValidator() =>
        RuleFor(x => x)
            .Must(x => CalendarRange.IsValid(x.From, x.To))
            .WithError(ApartmentErrors.InvalidCalendarRange);
}
