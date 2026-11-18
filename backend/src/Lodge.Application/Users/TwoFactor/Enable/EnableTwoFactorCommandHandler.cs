using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Users;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Users.TwoFactor.Enable;

internal sealed class EnableTwoFactorCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    TwoFactorCodeVerifier codeVerifier,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<EnableTwoFactorCommand, RecoveryCodesResponse>
{
    public async Task<Result<RecoveryCodesResponse>> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
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

        if (user.TwoFactorEnabled)
        {
            return Result.Failure<RecoveryCodesResponse>(TwoFactorErrors.AlreadyEnabled);
        }

        if (user.TwoFactorSecret is null)
        {
            return Result.Failure<RecoveryCodesResponse>(TwoFactorErrors.NotSetUp);
        }

        long? step = codeVerifier.VerifyTotp(user, request.Code);

        if (step is null)
        {
            return Result.Failure<RecoveryCodesResponse>(TwoFactorErrors.InvalidCode);
        }

        Result result = user.EnableTwoFactor(dateTimeProvider.UtcNow, step.Value);

        if (result.IsFailure)
        {
            return Result.Failure<RecoveryCodesResponse>(result.Error);
        }

        IReadOnlyList<string> recoveryCodes = await codeVerifier.ReplaceRecoveryCodesAsync(user, cancellationToken);

        return new RecoveryCodesResponse(recoveryCodes);
    }
}
