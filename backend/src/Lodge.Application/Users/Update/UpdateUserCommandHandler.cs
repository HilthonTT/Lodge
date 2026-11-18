using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Application.Core.Errors;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Users.Update;

internal sealed class UpdateUserCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    IBlobService blobService) : ICommandHandler<UpdateUserCommand>
{
    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure(UserErrors.InvalidPermissions);
        }

        Result<FirstName> firstNameResult = FirstName.Create(request.FirstName);
        Result<LastName> lastNameResult = LastName.Create(request.LastName);

        Result firstFailureOrSuccess = Result.FirstFailureOrSuccess(firstNameResult, lastNameResult);

        if (firstFailureOrSuccess.IsFailure)
        {
            return Result.Failure(firstFailureOrSuccess.Error);
        }

        User? user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(request.UserId));
        }

        if (request.Bio is not null)
        {
            Result bioResult = user.ChangeBio(request.Bio);

            if (bioResult.IsFailure)
            {
                return bioResult;
            }
        }

        user.ChangeName(firstNameResult.Value, lastNameResult.Value);

        if (request.ImageId is Guid imageId && imageId != user.ImageId)
        {
            // Only files uploaded by this user may be used as their profile picture.
            if (!await blobService.IsUserOwnerAsync(imageId, user.Id, cancellationToken))
            {
                return Result.Failure(BlobErrors.NotFound(imageId));
            }

            user.ChangeImage(imageId);
        }

        return Result.Success();
    }
}
