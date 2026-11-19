using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Users.TwoFactor;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Authentication.LoginTwoFactor;

internal sealed class LoginTwoFactorCommandHandler(
    IJwtProvider jwtProvider,
    IUserRepository userRepository,
    TwoFactorCodeVerifier codeVerifier,
    ICacheService cacheService) : ICommandHandler<LoginTwoFactorCommand, TokenResponse>
{
    /// <summary>
    /// Failed attempts allowed per user within the 10-minute window, so codes cannot be brute-forced.
    /// </summary>
    internal const int MaxAttempts = 5;

    public async Task<Result<TokenResponse>> Handle(LoginTwoFactorCommand request, CancellationToken cancellationToken)
    {
        TwoFactorChallenge? challenge = await jwtProvider.ValidateTwoFactorChallengeAsync(request.TwoFactorToken);

        if (challenge is null)
        {
            return Result.Failure<TokenResponse>(AuthenticationErrors.InvalidTwoFactorToken);
        }

        // Counted per user, not per challenge: otherwise logging in again would hand out a fresh set of attempts.
        string attemptsKey = $"two-factor-attempts:{challenge.UserId}";
        int failedAttempts = await cacheService.GetAsync<int>(attemptsKey, cancellationToken);

        if (failedAttempts >= MaxAttempts)
        {
            return Result.Failure<TokenResponse>(AuthenticationErrors.InvalidTwoFactorToken);
        }

        User? user = await userRepository.GetByIdAsync(challenge.UserId, cancellationToken);

        if (user is null || !user.TwoFactorEnabled)
        {
            return Result.Failure<TokenResponse>(AuthenticationErrors.InvalidTwoFactorToken);
        }

        bool verified = !string.IsNullOrWhiteSpace(request.Code)
            ? codeVerifier.VerifyAndRecordTotp(user, request.Code)
            : await codeVerifier.UseRecoveryCodeAsync(user, request.RecoveryCode, cancellationToken);

        if (!verified)
        {
            await cacheService.SetAsync(attemptsKey, failedAttempts + 1, TimeSpan.FromMinutes(10), cancellationToken);

            return Result.Failure<TokenResponse>(AuthenticationErrors.InvalidTwoFactorCode);
        }

        return new TokenResponse(jwtProvider.Create(user));
    }
}
