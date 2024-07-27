namespace Lodge.Application.Abstractions.Caching;

/// <summary>
/// Represents the cache service interface.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets the cached value using the specified key.
    /// </summary>
    /// <typeparam name="T">The cached value type.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cached value if found, otherwise null.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the value using the specified key.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="value">The value being cached.</param>
    /// <param name="expiration">The expiration time.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the cached value using the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all the cached value starting with the prefix.
    /// </summary>
    /// <param name="prefix">The prefix.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
