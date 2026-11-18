namespace Lodge.Application.Abstractions.Caching;

/// <summary>
/// Contains the cache keys.
/// </summary>
internal static class CacheKeys
{
    internal static class Apartments
    {
        internal static string GetPrice(Guid apartmentId, DateOnly startDate, DateOnly endDate) =>
            $"apartment-{apartmentId}-price-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}";

        internal static string GetById(Guid apartmentId) => $"apartment-{apartmentId}";
    }
}
