using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Users.Update;

/// <summary>
/// Represents the <see cref="UpdateUserCommand"/> handler.
/// </summary>
/// <param name="userIdentifierProvider">The user identifier provider.</param>
/// <param name="userRepository">The user repository.</param>
/// <param name="blobService">The blob service.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal class UpdateUserCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    IBlobService blobService,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateUserCommand>
{
    /// <inheritdoc />
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

        user.ChangeName(firstNameResult.Value, lastNameResult.Value);

        if (await ImageExistsAsync(request.ImageId, cancellationToken))
        {
            user.ChangeImage(request.ImageId!.Value);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Checks if the specified image identifier exists.
    /// </summary>
    /// <param name="imageId">The image identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the image exits otherwise false.</returns>
    private async Task<bool> ImageExistsAsync(Guid? imageId, CancellationToken cancellationToken)
    {
        if (!imageId.HasValue || imageId is null)
        {
            return false;
        }

        bool imageExists = await blobService.ExistsAsync(imageId.Value, cancellationToken);

        return imageExists;
    }
}
