using FluentValidation;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Apartments;

namespace Lodge.Application.Apartments.GetCalendar;

/// <summary>
/// Calendar ranges are inclusive and span at most <see cref="Apartment.MaxCalendarDays"/> days.
/// </summary>
internal static class CalendarRange
{
    public static bool IsValid(DateOnly from, DateOnly to) =>
        from <= to && to.DayNumber - from.DayNumber + 1 <= Apartment.MaxCalendarDays;
}

internal sealed class GetApartmentCalendarQueryValidator : AbstractValidator<GetApartmentCalendarQuery>
{
    public GetApartmentCalendarQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => CalendarRange.IsValid(x.From, x.To))
            .WithError(ApartmentErrors.InvalidCalendarRange);
    }
}
