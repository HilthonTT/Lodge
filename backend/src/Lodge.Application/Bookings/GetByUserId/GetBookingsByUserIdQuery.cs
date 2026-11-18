using Lodge.Application.Abstractions.Caching;
using Lodge.Contracts.Bookings;

namespace Lodge.Application.Bookings.GetByUserId;

/// <summary>
/// Represents the query for fetching the bookings by its user identifier.
/// </summary>
/// <param name="UserId">The user identifier.</param>
public sealed record GetBookingsByUserIdQuery(Guid UserId) : ICachedQuery<List<BookingResponse>>
{
    public string CacheKey => CacheKeys.Bookings.GetByUserId(UserId);

    public TimeSpan? Expiration => TimeSpan.FromMinutes(20);
}
