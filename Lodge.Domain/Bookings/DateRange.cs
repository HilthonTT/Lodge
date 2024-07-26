using Lodge.Domain.Core.Guards;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Represents the date range value object.
/// </summary>
public sealed record DateRange
{
    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public DateOnly Start { get; }

    public DateOnly End { get; }

    public int LengthInDays => End.DayNumber - Start.DayNumber;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <returns>The newly date range.</returns>
    public static DateRange Create(DateOnly start, DateOnly end)
    {
        Ensure.StartDatePrecedesEndDate(start, end, "The End date precedes start date");

        return new(start, end);
    }
}
