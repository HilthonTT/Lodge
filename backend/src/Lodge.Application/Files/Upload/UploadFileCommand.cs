using Lodge.Application.Abstractions.Idempotency;

namespace Lodge.Application.Files.Upload;

/// <summary>
/// Uploads an image owned by the specified user.
/// </summary>
public sealed record UploadFileCommand(
    Guid RequestId,
    Guid UserId,
    Stream Stream,
    long Length,
    string ContentType) : IdempotentCommand<Guid>(RequestId);
