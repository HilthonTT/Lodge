using System.Collections.Concurrent;
using Lodge.Application.Abstractions.Storage;
using Lodge.Contracts.Storage;

namespace Lodge.Api.FunctionalTests.Abstractions;

/// <summary>
/// An in-memory blob store, so functional tests never need Azure Storage / Azurite.
/// </summary>
public sealed class InMemoryBlobService : IBlobService
{
    private readonly ConcurrentDictionary<Guid, (byte[] Content, string ContentType, Guid UserId)> _blobs = new();

    public bool Contains(Guid fileId) => _blobs.ContainsKey(fileId);

    public Task<FileResponse> DownloadAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        (byte[] content, string contentType, _) = _blobs[fileId];

        return Task.FromResult(new FileResponse(new MemoryStream(content), contentType));
    }

    public async Task<Guid> UploadAsync(Guid userId, Stream stream, string contentType, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);

        Guid fileId = Guid.NewGuid();
        _blobs[fileId] = (buffer.ToArray(), contentType, userId);

        return fileId;
    }

    public Task DeleteAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        _blobs.TryRemove(fileId, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid fileId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_blobs.ContainsKey(fileId));

    public Task<bool> IsUserOwnerAsync(Guid fileId, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_blobs.TryGetValue(fileId, out var blob) && blob.UserId == userId);
}
