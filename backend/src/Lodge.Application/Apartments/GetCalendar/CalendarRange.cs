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
