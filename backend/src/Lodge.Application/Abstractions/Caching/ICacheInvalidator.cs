namespace Lodge.Application.Abstractions.Caching;

/// <summary>
/// Collects cache keys that a command makes stale. The keys are removed once the command's transaction has been
/// committed, so a concurrent read cannot re-cache the old data in between.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>
    /// Removes the specified cache key after the current command has been committed successfully.
    /// </summary>
    /// <param name="cacheKey">The cache key.</param>
    void InvalidateOnCommit(string cacheKey);
}
