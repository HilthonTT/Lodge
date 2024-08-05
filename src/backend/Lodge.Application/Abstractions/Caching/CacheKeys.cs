namespace Lodge.Application.Abstractions.Caching;

/// <summary>
/// Contains all the cache keys.
/// </summary>
internal static class CacheKeys
{
    /// <summary>
    /// Contains the apartment keys.
    /// </summary>
    internal static class Apartments
    {
        internal static string Search(DateOnly startOnly, DateOnly endDate) =>
            $"apartments-search-{startOnly.ToShortDateString()}-{endDate.ToShortDateString()}";

        internal static string GetById(Guid apartmentId) => $"apartment-{apartmentId}";
    }

    /// <summary>
    /// Contains the user keys.
    /// </summary>
    internal static class Users
    {
        internal static string GetById(Guid userId) => $"users-{userId}";
    }

    /// <summary>
    /// Contains the review keys.
    /// </summary>
    internal static class Reviews
    {
        internal static string GetById(Guid reviewId) => $"reviews-{reviewId}";
    }

    /// <summary>
    /// Contains the booking keys.
    /// </summary>
    internal static class Bookings
    {
        internal static string GetById(Guid bookingId) => $"booking-{bookingId}";
    }
}
