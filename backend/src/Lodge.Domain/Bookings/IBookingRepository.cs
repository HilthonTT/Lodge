using Lodge.Domain.Apartments;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Represents the booking repository interface
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Gets the booking with the specified identifier.
    /// </summary>
    /// <param name="id">The booking identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The instance that may contain the booking with the specified identifier.</returns>
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an active booking (see <see cref="BookingStatuses.Active"/>) of the apartment overlaps the
    /// specified stay. Overlaps are night-based: a stay may start on the day another one checks out.
    /// </summary>
    /// <param name="apartment">The apartment to check for overlapping bookings.</param>
    /// <param name="duration">The duration of the booking.</param>
    /// <param name="excludedBookingId">A booking to ignore (when changing its own dates).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if there is an overlap, otherwise false.</returns>
    Task<bool> IsOverlappingAsync(
        Apartment apartment,
        DateRange duration,
        Guid? excludedBookingId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the apartment has upcoming bookings (see <see cref="BookingStatuses.Upcoming"/>) that end
    /// after <paramref name="today"/>.
    /// </summary>
    Task<bool> HasUpcomingBookingsAsync(Guid apartmentId, DateOnly today, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts the specified booking to the database.
    /// </summary>
    /// <param name="booking">The booking to be inserted to the database.</param>
    void Insert(Booking booking);
}
