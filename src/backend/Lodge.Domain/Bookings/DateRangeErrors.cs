using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Contains all the date range errors
/// </summary>
public static class DateRangeErrors
{
    public static readonly Error StartDatePrecedesEndDate = Error.Problem(
        "DateRange.StartDatePrecedesEndDate", "The start date precedes the end date");
}
