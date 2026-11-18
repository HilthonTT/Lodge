using Lodge.Application.Abstractions.Cryptography;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Users.TwoFactor;

/// <summary>
/// Verifies TOTP codes (with replay protection) and recovery codes, and generates recovery codes.
/// </summary>
internal sealed class TwoFactorCodeVerifier(
    ISecretProtector secretProtector,
    ITotpProvider totpProvider,
    IRecoveryCodeRepository recoveryCodeRepository,
    IDateTimeProvider dateTimeProvider)
{
    /// <summary>
    /// Verifies a TOTP code against the user's (pending or enabled) secret.
    /// </summary>
    /// <returns>The matched time step, or null. Steps at or before the last accepted one are rejected.</returns>
    public long? VerifyTotp(User user, string? code)
    {
        if (user.TwoFactorSecret is null || string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        string? secret = secretProtector.Unprotect(user.TwoFactorSecret);

        if (secret is null)
        {
            return null;
        }

        long? step = totpProvider.Verify(secret, code, dateTimeProvider.UtcNow);

        return step is not null && (user.TwoFactorLastUsedStep is null || step > user.TwoFactorLastUsedStep)
            ? step
            : null;
    }

    /// <summary>
    /// Verifies a TOTP code and records its time step, so the same code cannot be used twice.
    /// </summary>
    public bool VerifyAndRecordTotp(User user, string? code)
    {
        long? step = VerifyTotp(user, code);

        if (step is null)
        {
            return false;
        }

        user.RecordTwoFactorStep(step.Value);

        return true;
    }

    /// <summary>
    /// Marks a matching, unused recovery code as used.
    /// </summary>
    public async Task<bool> UseRecoveryCodeAsync(User user, string? recoveryCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recoveryCode))
        {
            return false;
        }

        string hash = SecureTokens.HashRecoveryCode(recoveryCode);

        List<RecoveryCode> codes = await recoveryCodeRepository.GetByUserIdAsync(user.Id, cancellationToken);

        RecoveryCode? match = codes.Find(code => !code.IsUsed && code.CodeHash == hash);

        if (match is null)
        {
            return false;
        }

        match.Use(dateTimeProvider.UtcNow);

        return true;
    }

    /// <summary>
    /// Replaces the user's recovery codes with ten new ones and returns them in plain text (shown once).
    /// </summary>
    public async Task<IReadOnlyList<string>> ReplaceRecoveryCodesAsync(User user, CancellationToken cancellationToken)
    {
        await RemoveRecoveryCodesAsync(user, cancellationToken);

        string[] codes = Enumerable.Range(0, SecureTokens.RecoveryCodeCount)
            .Select(_ => SecureTokens.GenerateRecoveryCode())
            .ToArray();

        recoveryCodeRepository.InsertRange(codes.Select(code => RecoveryCode.Create(user.Id, SecureTokens.HashRecoveryCode(code))));

        return codes;
    }

    public async Task RemoveRecoveryCodesAsync(User user, CancellationToken cancellationToken) =>
        recoveryCodeRepository.RemoveRange(await recoveryCodeRepository.GetByUserIdAsync(user.Id, cancellationToken));
}
