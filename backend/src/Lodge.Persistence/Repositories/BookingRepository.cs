using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the booking repository.
/// </summary>
/// <param name="context">The database context.</param>
internal sealed class BookingRepository(LodgeDbContext context) : IBookingRepository
{
    private static readonly BookingStatus[] ActiveStatuses = [.. BookingStatuses.Active];

    private static readonly BookingStatus[] UpcomingStatuses = [.. BookingStatuses.Upcoming];

    /// <inheritdoc />
    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Bookings.FirstOrDefaultAsync(booking => booking.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsOverlappingAsync(
        Apartment apartment,
        DateRange duration,
        Guid? excludedBookingId = null,
        CancellationToken cancellationToken = default)
    {
        Guid excluded = excludedBookingId ?? Guid.Empty;

        return context.Bookings.AnyAsync(
            booking =>
                booking.ApartmentId == apartment.Id &&
                booking.Id != excluded &&
                // Night-based: a stay may start on the day another one checks out.
                booking.Duration.Start < duration.End &&
                booking.Duration.End > duration.Start &&
                ActiveStatuses.Contains(booking.Status),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> HasUpcomingBookingsAsync(Guid apartmentId, DateOnly today, CancellationToken cancellationToken = default) =>
        context.Bookings.AnyAsync(
            booking =>
                booking.ApartmentId == apartmentId &&
                booking.Duration.End > today &&
                UpcomingStatuses.Contains(booking.Status),
            cancellationToken);

    /// <inheritdoc />
    public void Insert(Booking booking) => context.Bookings.Add(booking);
}
