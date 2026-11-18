using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
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

        Guid fileId = await blobService.UploadAsync(
            request.UserId,
            request.Stream,
            request.ContentType,
            cancellationToken);

        return fileId;
    }
}
