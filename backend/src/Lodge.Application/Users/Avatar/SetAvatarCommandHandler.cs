using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Contracts.Users;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Users.Avatar;

internal sealed class SetAvatarCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    IBlobService blobService,
    IOptions<AppOptions> appOptions) : ICommandHandler<SetAvatarCommand, AvatarResponse>
{
    public async Task<Result<AvatarResponse>> Handle(SetAvatarCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<AvatarResponse>(UserErrors.InvalidPermissions);
        }

        User? user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AvatarResponse>(UserErrors.NotFound(request.UserId));
        }

        // Buffer the (at most 5 MB) file: the declared content type is not trusted, the bytes are checked.
        Result<BufferedImage> image = await ImageFormats.BufferAsync(
            request.Stream,
            SetAvatarCommandValidator.MaxFileSizeInBytes,
            UserErrors.InvalidAvatar,
            UserErrors.AvatarTooLarge,
            cancellationToken);

        if (image.IsFailure)
        {
            return Result.Failure<AvatarResponse>(image.Error);
        }

        Guid fileId;

        using (BufferedImage buffered = image.Value)
        {
            fileId = await blobService.UploadAsync(user.Id, buffered.Content, buffered.ContentType, cancellationToken);
        }

        Guid? previousImageId = user.ImageId;

        user.ChangeImage(fileId);

        if (previousImageId is Guid previous)
        {
            await blobService.DeleteAsync(previous, cancellationToken);
        }

        return new AvatarResponse(appOptions.Value.FileUrl(fileId));
    }
}
