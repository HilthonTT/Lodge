using Lodge.Domain.Bookings;

namespace Lodge.Domain.Apartments;

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
