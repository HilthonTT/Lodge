using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Represents the date range value object.
/// </summary>
public sealed record DateRange
{
    /// <summary>
    /// Initializes a new instance of <see cref="DateRange"/> record.
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateRange"/> record.
    /// </summary>
    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private DateRange()
    {
    }

    /// <summary>
    /// Gets the start date.
    /// </summary>
    public DateOnly Start { get; }

    /// <summary>
    /// Gets the end date.
    /// </summary>

    public DateOnly End { get; }

    /// <summary>
    /// Gets the length of days between the end date and the start date.
    /// </summary>
    public int LengthInDays => End.DayNumber - Start.DayNumber;

    /// <summary>
    /// The date range creation process containing the date range or may throw an exception if the date range is invalid.
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <returns>The newly date range.</returns>
    public static Result<DateRange> Create(DateOnly start, DateOnly end)
    {
        if (start >= end)
        {
            return Result.Failure<DateRange>(DateRangeErrors.StartDatePrecedesEndDate);
        }

        return new DateRange(start, end);
    }
}
