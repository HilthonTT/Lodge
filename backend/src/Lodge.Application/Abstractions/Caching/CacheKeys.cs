namespace Lodge.Application.Abstractions.Caching;

/// <summary>
/// Contains the cache keys.
/// </summary>
internal static class CacheKeys
{
    internal static class Apartments
    {
        internal static string GetById(Guid apartmentId) => $"apartment-{apartmentId}";
    }
}
