using Lodge.Domain.Apartments;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the calendar day repository.
/// </summary>
/// <param name="context">The database context.</param>
internal sealed class CalendarDayRepository(LodgeDbContext context) : ICalendarDayRepository
{
    /// <inheritdoc />
    public Task<List<CalendarDay>> GetRangeAsync(
        Guid apartmentId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default) =>
        context.CalendarDays
            .Where(day => day.ApartmentId == apartmentId && day.Date >= from && day.Date <= to)
            .OrderBy(day => day.Date)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> AnyBlockedAsync(
        Guid apartmentId,
        DateOnly checkIn,
        DateOnly checkOut,
        CancellationToken cancellationToken = default) =>
        context.CalendarDays.AnyAsync(
            day => day.ApartmentId == apartmentId && day.IsBlocked && day.Date >= checkIn && day.Date < checkOut,
            cancellationToken);

    /// <inheritdoc />
    public Task<Dictionary<DateOnly, decimal>> GetPriceOverridesAsync(
        Guid apartmentId,
        DateOnly checkIn,
        DateOnly checkOut,
        CancellationToken cancellationToken = default) =>
        context.CalendarDays
            .AsNoTracking()
            .Where(day => day.ApartmentId == apartmentId && day.Price != null && day.Date >= checkIn && day.Date < checkOut)
            .ToDictionaryAsync(day => day.Date, day => day.Price!.Value, cancellationToken);

    /// <inheritdoc />
    public void Insert(CalendarDay day) => context.CalendarDays.Add(day);

    /// <inheritdoc />
    public void Remove(CalendarDay day) => context.CalendarDays.Remove(day);
}
