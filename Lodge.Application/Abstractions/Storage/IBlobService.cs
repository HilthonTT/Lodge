using Lodge.Contracts.Storage;

namespace Lodge.Application.Abstractions.Storage;

/// <summary>
/// Represents the blob service interface.
/// </summary>
public interface IBlobService
{
    /// <summary>
    /// Downloads a file as a stream based on the provided file identifier.
    /// </summary>
    /// <param name="fileId">The identifier of the file to download.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The completed task that represents the asynchronous download operation. 
    /// The task result contains the file response.
    /// </returns>
    Task<FileResponse> DownloadAsync(Guid fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a stream as a file to the blob storage.
    /// </summary>
    /// <param name="stream">The stream of the file to upload.</param>
    /// <param name="contentType">The MIME type of the file to upload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The completed task that represents the asynchronous upload operation. 
    /// The task result contains the identifier of the uploaded file.
    /// </returns>
    Task<Guid> UploadAsync(Stream stream, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the blob storage based on the provided file identifier.
    /// </summary>
    /// <param name="fileId">The identifier of the file to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task DeleteAsync(Guid fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in the blob storage based on the provided file identifier.
    /// </summary>
    /// <param name="fileId">The identifier of the file to check existence.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The completed task that represents the asynchronous operation. 
    /// The task result contains a boolean indicating whether the file exists.
    /// </returns>
    Task<bool> ExistsAsync(Guid fileId, CancellationToken cancellationToken = default);
}
