using System.Globalization;
using Lodge.Domain.Bookings;

namespace Lodge.Domain.Apartments;

/// <summary>
/// The length of a flexible-date search ("a weekend / a week / a month in December or January").
/// </summary>
public enum FlexibleStayLength
{
    /// <summary>
    /// Friday to Sunday (2 nights).
    /// </summary>
    Weekend = 1,

    /// <summary>
    /// 7 nights, any start day.
    /// </summary>
    Week = 2,

    /// <summary>
    /// 28 nights, any start day.
    /// </summary>
    Month = 3,
}

/// <summary>
/// A calendar month, written <c>yyyy-MM</c> (e.g. <c>2026-12</c>).
/// </summary>
public readonly record struct YearMonth(int Year, int Month)
{
    public DateOnly FirstDay => new(Year, Month, 1);

    public int DayCount => DateTime.DaysInMonth(Year, Month);

    public static bool TryParse(string? value, out YearMonth yearMonth)
    {
        yearMonth = default;

        if (!DateOnly.TryParseExact(
                value?.Trim(),
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly date))
        {
            return false;
        }

        yearMonth = new YearMonth(date.Year, date.Month);

        return true;
    }

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}

/// <summary>
/// Finds stay windows for flexible-date searches. A window "is in" a month when its check-in date is in that month
/// (the stay may end in the next month). Windows never start before today.
/// </summary>
public static class FlexibleStay
{
    public static int Nights(FlexibleStayLength length) => length switch
    {
        FlexibleStayLength.Weekend => 2,
        FlexibleStayLength.Week => 7,
        FlexibleStayLength.Month => 28,
        _ => throw new ArgumentOutOfRangeException(nameof(length), length, "Unknown flexible stay length."),
    };

    public static bool TryParseLength(string? value, out FlexibleStayLength length)
    {
        length = default;

        if (string.IsNullOrWhiteSpace(value) ||
            !Enum.TryParse(value.Trim(), ignoreCase: true, out length) ||
            !Enum.IsDefined(length))
        {
            length = default;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets every possible check-in date, in order: every Friday of the months for weekends, every day otherwise.
    /// </summary>
    public static IReadOnlyList<DateOnly> CandidateCheckIns(
        FlexibleStayLength length,
        IEnumerable<YearMonth> months,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(months);

        var checkIns = new SortedSet<DateOnly>();

        foreach (YearMonth month in months)
        {
            for (int day = 0; day < month.DayCount; day++)
            {
                DateOnly date = month.FirstDay.AddDays(day);

                if (date < today || (length == FlexibleStayLength.Weekend && date.DayOfWeek != DayOfWeek.Friday))
                {
                    continue;
                }

                checkIns.Add(date);
            }
        }

        return [.. checkIns];
    }

    /// <summary>
    /// Finds the earliest free window of the requested length, or null when there is none.
    /// </summary>
    /// <param name="length">The stay length.</param>
    /// <param name="months">The months the stay may start in.</param>
    /// <param name="today">Today; earlier check-ins are ignored.</param>
    /// <param name="isNightAvailable">Tells whether the night starting on a date is free.</param>
    public static DateRange? FindFirstWindow(
        FlexibleStayLength length,
        IEnumerable<YearMonth> months,
        DateOnly today,
        Func<DateOnly, bool> isNightAvailable)
    {
        ArgumentNullException.ThrowIfNull(isNightAvailable);

        int nights = Nights(length);

        foreach (DateOnly checkIn in CandidateCheckIns(length, months, today))
        {
            bool free = true;

            for (int night = 0; night < nights && free; night++)
            {
                free = isNightAvailable(checkIn.AddDays(night));
            }

            if (free)
            {
                return DateRange.Create(checkIn, checkIn.AddDays(nights)).Value;
            }
        }

        return null;
    }
}
