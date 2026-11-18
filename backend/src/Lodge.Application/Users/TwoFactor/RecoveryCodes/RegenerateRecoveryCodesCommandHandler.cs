using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Users;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Users.TwoFactor.RecoveryCodes;

internal sealed class RegenerateRecoveryCodesCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    TwoFactorCodeVerifier codeVerifier) : ICommandHandler<RegenerateRecoveryCodesCommand, RecoveryCodesResponse>
{
    public async Task<Result<RecoveryCodesResponse>> Handle(
        RegenerateRecoveryCodesCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<RecoveryCodesResponse>(UserErrors.InvalidPermissions);
        }

        User? user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<RecoveryCodesResponse>(UserErrors.NotFound(request.UserId));
        }

        if (!user.TwoFactorEnabled)
        {
            return Result.Failure<RecoveryCodesResponse>(TwoFactorErrors.NotEnabled);
        }

        if (!codeVerifier.VerifyAndRecordTotp(user, request.Code))
        {
            return Result.Failure<RecoveryCodesResponse>(TwoFactorErrors.InvalidCode);
        }

        IReadOnlyList<string> recoveryCodes = await codeVerifier.ReplaceRecoveryCodesAsync(user, cancellationToken);

        return new RecoveryCodesResponse(recoveryCodes);
    }
}
