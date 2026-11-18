using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Cryptography;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Users.ChangePassword;

internal sealed class ChangePasswordCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IPasswordHashChecker passwordHashChecker) : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure(UserErrors.InvalidPermissions);
        }

        Result<Password> passwordResult = Password.Create(request.Password);

        if (passwordResult.IsFailure)
        {
            return Result.Failure(passwordResult.Error);
        }

        User? user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(request.UserId));
        }

        // Accounts created through a social login set their first password through "forgot password".
        if (!user.HasPassword)
        {
            return Result.Failure(UserErrors.PasswordNotSet);
        }

        // Hashes are salted, so the only way to detect "same password" is to verify it.
        if (user.VerifyPasswordHash(request.Password, passwordHashChecker))
        {
            return Result.Failure(UserErrors.CannotChangePassword);
        }

        user.ChangePassword(passwordHasher.HashPassword(passwordResult.Value));

        return Result.Success();
    }
}
