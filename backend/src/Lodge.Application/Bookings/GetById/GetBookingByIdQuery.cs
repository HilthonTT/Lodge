using Lodge.Application.Abstractions.Caching;
using Lodge.Contracts.Bookings;

namespace Lodge.Application.Bookings.GetById;

/// <summary>
/// Represents the query for getting a booking by its identifier.
/// </summary>
/// <param name="BookingId">The booking identifier.</param>
public sealed record GetBookingByIdQuery(Guid BookingId) : ICachedQuery<BookingResponse>
{
    public string CacheKey => CacheKeys.Bookings.GetById(BookingId);

    public TimeSpan? Expiration => TimeSpan.FromMinutes(20);
}
