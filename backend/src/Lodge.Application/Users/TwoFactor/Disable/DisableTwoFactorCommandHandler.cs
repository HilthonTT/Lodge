using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Users.TwoFactor.Disable;

internal sealed class DisableTwoFactorCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    IPasswordHashChecker passwordHashChecker,
    TwoFactorCodeVerifier codeVerifier) : ICommandHandler<DisableTwoFactorCommand>
{
    public async Task<Result> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
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

        if (!user.TwoFactorEnabled)
        {
            return Result.Failure(TwoFactorErrors.NotEnabled);
        }

        // Social-only accounts have no password, so the code alone is enough for them.
        if (user.HasPassword && !user.VerifyPasswordHash(request.Password ?? string.Empty, passwordHashChecker))
        {
            return Result.Failure(TwoFactorErrors.InvalidPassword);
        }

        if (!codeVerifier.VerifyAndRecordTotp(user, request.Code))
        {
            return Result.Failure(TwoFactorErrors.InvalidCode);
        }

        user.DisableTwoFactor();

        await codeVerifier.RemoveRecoveryCodesAsync(user, cancellationToken);

        return Result.Success();
    }
}
