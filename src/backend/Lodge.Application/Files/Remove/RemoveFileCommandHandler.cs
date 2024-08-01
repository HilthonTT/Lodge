using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Contracts.Storage;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Files.Remove;

/// <summary>
/// Represents the <see cref="RemoveFileCommand"/> handler.
/// </summary>
/// <param name="userIdentifierProvider">The user identifier provider.</param>
/// <param name="blobService">The blob service.</param>
internal sealed class RemoveFileCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IBlobService blobService) : ICommandHandler<RemoveFileCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RemoveFileCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure(UserErrors.InvalidPermissions);
        }

        if (!await blobService.IsUserOwnerAsync(request.FileId, request.UserId, cancellationToken))
        {
            return Result.Failure<FileResponse>(UserErrors.InvalidPermissions);
        }

        await blobService.DeleteAsync(request.FileId, cancellationToken);

        return Result.Success();
    }
}
