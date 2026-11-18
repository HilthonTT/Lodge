using System.Globalization;

namespace Lodge.Domain.Apartments;

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
