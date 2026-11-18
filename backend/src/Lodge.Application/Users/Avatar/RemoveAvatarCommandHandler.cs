using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Users.Avatar;

internal sealed class RemoveAvatarCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    IBlobService blobService) : ICommandHandler<RemoveAvatarCommand>
{
    public async Task<Result> Handle(RemoveAvatarCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure(UserErrors.InvalidPermissions);
        }

        User? user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(request.UserId));
        }

        if (user.ImageId is Guid imageId)
        {
            user.RemoveImage();

            await blobService.DeleteAsync(imageId, cancellationToken);
        }

        return Result.Success();
    }
}
