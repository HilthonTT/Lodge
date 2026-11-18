using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Lodge.Application.Abstractions.Storage;
using Lodge.Contracts.Storage;
using Microsoft.Extensions.Options;

namespace Lodge.Infrastructure.Storage;

/// <summary>
/// Stores user files in Azure Blob Storage (Azurite locally). The uploader's id is kept in the blob metadata.
/// </summary>
internal sealed class BlobService(BlobServiceClient blobServiceClient, IOptions<BlobStorageOptions> options) : IBlobService
{
    private const string UserIdKey = "UserId";

    private readonly SemaphoreSlim _containerLock = new(1, 1);
    private bool _containerCreated;

    public async Task<Guid> UploadAsync(
        Guid userId,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = await GetContainerAsync(cancellationToken);

        Guid fileId = Guid.NewGuid();

        await containerClient.GetBlobClient(fileId.ToString()).UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                Metadata = new Dictionary<string, string> { [UserIdKey] = userId.ToString() },
            },
            cancellationToken);

        return fileId;
    }

    public async Task<FileResponse> DownloadAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        Response<BlobDownloadStreamingResult> response = await GetBlobClient(fileId)
            .DownloadStreamingAsync(cancellationToken: cancellationToken);

        return new FileResponse(response.Value.Content, response.Value.Details.ContentType);
    }

    public Task DeleteAsync(Guid fileId, CancellationToken cancellationToken = default) =>
        GetBlobClient(fileId).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    public async Task<bool> ExistsAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        Response<bool> response = await GetBlobClient(fileId).ExistsAsync(cancellationToken);

        return response.Value;
    }

    public async Task<bool> IsUserOwnerAsync(Guid fileId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            BlobProperties properties = await GetBlobClient(fileId).GetPropertiesAsync(cancellationToken: cancellationToken);

            return properties.Metadata.TryGetValue(UserIdKey, out string? storedUserId) &&
                   storedUserId == userId.ToString();
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return false;
        }
    }

    private BlobClient GetBlobClient(Guid fileId) =>
        blobServiceClient.GetBlobContainerClient(options.Value.ContainerName).GetBlobClient(fileId.ToString());

    private async Task<BlobContainerClient> GetContainerAsync(CancellationToken cancellationToken)
    {
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);

        if (_containerCreated)
        {
            return containerClient;
        }

        await _containerLock.WaitAsync(cancellationToken);

        try
        {
            if (!_containerCreated)
            {
                await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                _containerCreated = true;
            }
        }
        finally
        {
            _containerLock.Release();
        }

        return containerClient;
    }
}
