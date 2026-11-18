namespace Lodge.Domain.Apartments;

/// <summary>
/// Stores the hosts' calendar settings (blocked nights and price overrides).
/// </summary>
public interface ICalendarDayRepository
{
    /// <summary>
    /// Gets the stored days of the apartment from <paramref name="from"/> up to and including <paramref name="to"/>.
    /// </summary>
    Task<List<CalendarDay>> GetRangeAsync(
        Guid apartmentId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the host has blocked any night of the stay (check-in up to, excluding, check-out).
    /// </summary>
    Task<bool> AnyBlockedAsync(
        Guid apartmentId,
        DateOnly checkIn,
        DateOnly checkOut,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the custom nightly prices for the nights of the stay (check-in up to, excluding, check-out).
    /// </summary>
    Task<Dictionary<DateOnly, decimal>> GetPriceOverridesAsync(
        Guid apartmentId,
        DateOnly checkIn,
        DateOnly checkOut,
        CancellationToken cancellationToken = default);

    void Insert(CalendarDay day);

    void Remove(CalendarDay day);
}
