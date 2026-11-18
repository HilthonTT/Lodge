using System.Data;
using Dapper;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;

namespace Lodge.Application.Apartments;

/// <summary>
/// Contains the Dapper read queries for a listing's availability.
/// </summary>
internal static class ApartmentQueries
{
    private static readonly DateOnly MinDate = new(1900, 1, 1);
    private static readonly DateOnly MaxDate = new(9999, 1, 1);

    /// <summary>
    /// Returns every unavailable night: nights of active bookings (check-in up to, but excluding, the check-out
    /// day) and nights blocked by the host. A new stay may check out on an unavailable night or check in on another
    /// stay's check-out day.
    /// </summary>
    public static async Task<List<DateOnly>> GetUnavailableNightsAsync(IDbConnection connection, Guid apartmentId)
    {
        ListingNights nights = await GetNightsAsync(connection, apartmentId, MinDate, MaxDate);

        return [.. nights.Booked.Union(nights.Settings.Where(day => day.Value.Blocked).Select(day => day.Key)).Order()];
    }

    /// <summary>
    /// Gets the booked nights and the host's settings (blocks and custom prices) between <paramref name="from"/> and
    /// <paramref name="to"/> (both inclusive).
    /// </summary>
    public static async Task<ListingNights> GetNightsAsync(
        IDbConnection connection,
        Guid apartmentId,
        DateOnly from,
        DateOnly to)
    {
        const string sql =
            """
            -- Dates are read as timestamps: Dapper cannot convert the DateOnly values Npgsql returns for 'date'.
            SELECT DISTINCT d::timestamp AS Night
            FROM bookings AS b
            CROSS JOIN LATERAL generate_series(
                GREATEST(b.duration_start, @From::date),
                LEAST(b.duration_end, @EndExclusive::date) - 1,
                interval '1 day') AS d
            WHERE b.apartment_id = @ApartmentId
              AND b.status = ANY(@ActiveStatuses)
              AND b.duration_start < @EndExclusive::date
              AND b.duration_end > @From::date;

            SELECT c.date::timestamp AS Date, c.is_blocked AS IsBlocked, c.price AS Price
            FROM apartment_calendar_days AS c
            WHERE c.apartment_id = @ApartmentId
              AND c.date >= @From::date
              AND c.date < @EndExclusive::date;
            """;

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            sql,
            new
            {
                ApartmentId = apartmentId,
                From = from.ToDateTime(TimeOnly.MinValue),
                EndExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue),
                ActiveStatuses = BookingStatuses.ActiveValues,
            });

        HashSet<DateOnly> booked = [.. (await grid.ReadAsync<DateTime>()).Select(DateOnly.FromDateTime)];

        Dictionary<DateOnly, (bool Blocked, decimal? Price)> settings = (await grid.ReadAsync<CalendarDayRow>())
            .ToDictionary(row => DateOnly.FromDateTime(row.Date), row => (row.IsBlocked, row.Price));

        return new ListingNights(booked, settings);
    }

    /// <summary>
    /// Builds the night-by-night calendar of <paramref name="apartment"/>.
    /// </summary>
    public static IEnumerable<CalendarNight> BuildCalendar(
        Apartment apartment,
        ListingNights nights,
        DateOnly from,
        DateOnly to,
        DateOnly today)
    {
        for (DateOnly date = from; date <= to; date = date.AddDays(1))
        {
            bool booked = nights.Booked.Contains(date);
            (bool blocked, decimal? customPrice) = nights.Settings.GetValueOrDefault(date);

            yield return new CalendarNight(
                date,
                Available: !booked && !blocked && date >= today,
                blocked,
                booked,
                apartment.GetNightlyPrice(date, customPrice),
                customPrice is not null);
        }
    }

    internal sealed record ListingNights(
        HashSet<DateOnly> Booked,
        Dictionary<DateOnly, (bool Blocked, decimal? Price)> Settings);

    internal sealed record CalendarNight(
        DateOnly Date,
        bool Available,
        bool Blocked,
        bool Booked,
        decimal Price,
        bool CustomPrice);

    private sealed class CalendarDayRow
    {
        public DateTime Date { get; init; }

        public bool IsBlocked { get; init; }

        public decimal? Price { get; init; }
    }
}
