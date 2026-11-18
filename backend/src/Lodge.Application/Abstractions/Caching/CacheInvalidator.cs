namespace Lodge.Application.Abstractions.Caching;

/// <summary>
/// The per-request (scoped) list of cache keys to remove after the command commits.
/// </summary>
internal sealed class CacheInvalidator : ICacheInvalidator
{
    private readonly HashSet<string> _pendingKeys = new(StringComparer.Ordinal);

    public void InvalidateOnCommit(string cacheKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        _pendingKeys.Add(cacheKey);
    }

    /// <summary>
    /// Returns and clears the pending keys.
    /// </summary>
    internal IReadOnlyList<string> Drain()
    {
        string[] keys = [.. _pendingKeys];
        _pendingKeys.Clear();

        return keys;
    }
}
