using Lodge.Domain.Apartements;
using Lodge.Domain.Core.Primitives.Maybe;

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
    /// <returns>The maybe instance that may contain the booking with the specified identifier.</returns>
    Task<Maybe<Booking>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a booking is overlapping with the specified apartment and duration.
    /// </summary>
    /// <param name="apartment">The apartment to check for overlapping bookings.</param>
    /// <param name="duration">The duration of the booking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation that returns true if there is an overlap, otherwise false.</returns>
    Task<bool> IsOverlappingAsync(
        Apartment apartment,
        DateRange duration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds the specified booking to the database.
    /// </summary>
    /// <param name="booking">The booking to be added to the database.</param>
    void Add(Booking booking);
}