using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Hosting;

namespace Lodge.Application.Hosting.Photos;

/// <summary>
/// Uploads a listing photo (JPEG, PNG or WebP, detected from the bytes; at most 10 MB). The returned file id is then
/// passed in a listing's <c>imageIds</c>.
/// </summary>
public sealed record UploadListingPhotoCommand(Stream Stream, long Length) : ICommand<PhotoUploadResponse>
{
    public const long MaxFileSizeInBytes = 10 * 1024 * 1024;
}
