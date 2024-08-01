using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Domain.Core.Primitives;

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
        Guid fileId = await blobService.UploadAsync(
            userIdentifierProvider.UserId,
            request.Stream,
            request.ContentType,
            cancellationToken);

        return fileId;
    }
}
