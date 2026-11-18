using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Contains the date range errors.
/// </summary>
public static class DateRangeErrors
{
    public static readonly Error StartDatePrecedesEndDate = Error.Problem(
        "DateRange.StartDatePrecedesEndDate",
        "The end date must be after the start date");
}
