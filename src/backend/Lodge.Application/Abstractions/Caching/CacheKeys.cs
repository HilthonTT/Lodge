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
        internal static string GetById(Guid apartmentId) => $"apartment-{apartmentId}";
    }
}
