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
        internal static string GetPrice(Guid apartmentId) => $"apartment-{apartmentId}-price";

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
        internal static string GetByUserId(Guid userId) => $"bookings-user-{userId}";

        internal static string GetById(Guid bookingId) => $"booking-{bookingId}";
    }
}
