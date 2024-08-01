using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Lodge.Application.Abstractions.Storage;
using Lodge.Contracts.Storage;
using Lodge.Infrastructure.Storage.Settings;
using Microsoft.Extensions.Options;

namespace Lodge.Infrastructure.Storage;

/// <summary>
/// Represents the blob service.
/// </summary>
/// <param name="blobServiceClient">The blob service client.</param>
/// <param name="options">The blob service settings.</param>
internal sealed class BlobService(
    BlobServiceClient blobServiceClient, 
    IOptions<BlobServiceSettings> options) : IBlobService
{
    private const string UserIdKey = "UserId";

    private readonly BlobServiceSettings _blobServiceSettings = options.Value;

    /// <inheritdoc />
    public async Task<Guid> UploadAsync(
        Guid userId, 
        Stream stream, 
        string contentType, 
        CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_blobServiceSettings.ContainerName);

        Guid fileId = Guid.NewGuid();

        BlobClient blobClient = containerClient.GetBlobClient(fileId.ToString());

        var metaData = new Dictionary<string, string>
        {
            { UserIdKey, userId.ToString() }
        };

        await blobClient.UploadAsync(
            stream, 
            new BlobHttpHeaders { ContentType = contentType },
            metaData,
            cancellationToken: cancellationToken);

        return fileId;
    }

    /// <inheritdoc />
    public async Task<FileResponse> DownloadAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_blobServiceSettings.ContainerName);

        BlobClient blobClient = containerClient.GetBlobClient(fileId.ToString());

        Response<BlobDownloadResult> response = await blobClient.DownloadContentAsync(cancellationToken: cancellationToken);

        return new FileResponse(response.Value.Content.ToStream(), response.Value.Details.ContentType);
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid fileId,  CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_blobServiceSettings.ContainerName);

        BlobClient blobClient = containerClient.GetBlobClient(fileId.ToString());

        return blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_blobServiceSettings.ContainerName);

        BlobClient blobClient = containerClient.GetBlobClient(fileId.ToString());

        Response<bool> response = await blobClient.ExistsAsync(cancellationToken: cancellationToken);

        return response.Value;
    }

    /// <inheritdoc />
    public async Task<bool> IsUserOwnerAsync(Guid fileId, Guid userId, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_blobServiceSettings.ContainerName);

        BlobClient blobClient = containerClient.GetBlobClient(fileId.ToString());

        try
        {
            BlobProperties properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            if (!properties.Metadata.TryGetValue(UserIdKey, out string? storedUserId))
            {
                return false;
            }

            if (storedUserId == userId.ToString())
            {
                return true;
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Handle the case where the blob doesn't exist
            return false;
        }

        return false;
    }
}
