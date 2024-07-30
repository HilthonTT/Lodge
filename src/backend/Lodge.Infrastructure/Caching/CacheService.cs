using Lodge.Application.Abstractions.Caching;
using Lodge.Infrastructure.Caching.Settings;
using Microsoft.Extensions.Caching.Distributed;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Lodge.Infrastructure.Caching;

/// <summary>
/// Represents the cache service.
/// </summary>
/// <param name="cache">The cache.</param>
internal sealed class CacheService(IDistributedCache cache) : ICacheService
{
    private static readonly ConcurrentDictionary<string, bool> CacheKeys = [];

    /// <inheritdoc />  
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        byte[]? bytes = await cache.GetAsync(key, cancellationToken);

        return bytes is null ? default : Deserialize<T>(bytes);
    }

    /// <inheritdoc />  
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await  cache.RemoveAsync(key, cancellationToken);

        CacheKeys.TryRemove(key, out var _);
    }

    /// <inheritdoc />  
    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        IEnumerable<Task> tasks = CacheKeys
            .Keys
            .Where(k => k.StartsWith(prefix))
            .Select(k => RemoveAsync(k, cancellationToken));

        return Task.WhenAll(tasks);
    }

    /// <inheritdoc />  
    public async Task SetAsync<T>(
        string key, 
        T value, 
        TimeSpan? expiration = null, 
        CancellationToken cancellationToken = default)
    {
        byte[] bytes = Serialize(value);

        DistributedCacheEntryOptions options = CacheSettings.Create(expiration);

        await cache.SetAsync(key, bytes, options, cancellationToken);

        CacheKeys.TryAdd(key, true);
    }

    /// <summary>
    /// Deserializes the provided byte array to an object of type T.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
    /// <param name="bytes">The byte array to deserialize.</param>
    /// <returns>The deserialized object of type T.</returns>
    private static T Deserialize<T>(byte[] bytes)
    {
        return JsonSerializer.Deserialize<T>(bytes)!;
    }

    /// <summary>
    /// Serializes the provided object to a byte array.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>The serialized byte array.</returns>
    private static byte[] Serialize<T>(T value)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        JsonSerializer.Serialize(writer, value);

        return buffer.WrittenSpan.ToArray();
    }
}
