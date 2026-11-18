using Microsoft.AspNetCore.Http;

namespace Lodge.Contracts.Storage;

/// <summary>
/// Represents the upload file request.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="File">The file.</param>
public sealed record UploadFileRequest(Guid UserId, IFormFile File);
