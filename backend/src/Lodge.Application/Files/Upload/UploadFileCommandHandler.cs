using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Application.Core.Errors;
using Lodge.Application.Users.Avatar;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Files.Upload;

/// <summary>
/// Represents the <see cref="UploadFileCommand"/> handler.
/// </summary>
/// <param name="userIdentifierProvider">The user identifier provider.</param>
/// <param name="blobService">The blob service.</param>
internal sealed class UploadFileCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IBlobService blobService) : ICommandHandler<UploadFileCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<Guid>(UserErrors.InvalidPermissions);
        }

        // Same checks as avatars and listing photos: the leading bytes must be a JPEG, PNG or WebP image, and the
        // stored content type is the detected one, not the client's.
        Result<BufferedImage> image = await ImageFormats.BufferAsync(
            request.Stream,
            UploadFileCommandValidator.MaxFileSizeInBytes,
            ValidationErrors.UploadFile.InvalidContentType,
            ValidationErrors.UploadFile.FileTooLarge(UploadFileCommandValidator.MaxFileSizeInBytes),
            cancellationToken);

        if (image.IsFailure)
        {
            return Result.Failure<Guid>(image.Error);
        }

        using BufferedImage buffered = image.Value;

        Guid fileId = await blobService.UploadAsync(
            request.UserId,
            buffered.Content,
            buffered.ContentType,
            cancellationToken);

        return fileId;
    }
}
