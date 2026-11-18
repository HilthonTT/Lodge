using Microsoft.Extensions.Caching.Distributed;

namespace Lodge.Infrastructure.Caching.Settings;

/// <summary>
/// Represents the cache settings.
/// </summary>
public static class CacheSettings
{
    public const string SettingsKey = "Cache";

    /// <summary>
    /// Represents the default expiration.
    /// </summary>
    public static readonly DistributedCacheEntryOptions DefaultExpiration = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedCacheEntryOptions"/> class.
    /// </summary>
    /// <param name="expiration">The expiration.</param>
    /// <returns>A new instance of the <see cref="DistributedCacheEntryOptions" /> class.</returns>
    public static DistributedCacheEntryOptions Create(TimeSpan? expiration)
    {
        if (expiration is null)
        {
            return DefaultExpiration;
        }

        return new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
    }
}
