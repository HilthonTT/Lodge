using Lodge.Application.Abstractions.Idempotency;

namespace Lodge.Application.Files.Upload;

/// <summary>
/// Represents the upload file command.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="RequestId">The request identifier.</param>
/// <param name="Stream">The file stream.</param>
/// <param name="ContentType">The file content type.</param>
public sealed record UploadFileCommand(
    Guid RequestId,
    Guid UserId,
    Stream Stream,
    string ContentType) : IdempotentCommand<Guid>(RequestId);
