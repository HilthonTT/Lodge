namespace Lodge.Contracts.Authentication;

/// <summary>
/// Completes a login that requires a second factor. Send either <see cref="Code"/> or <see cref="RecoveryCode"/>.
/// </summary>
/// <param name="TwoFactorToken">The challenge token returned by the login.</param>
/// <param name="Code">The 6-digit code of the authenticator app.</param>
/// <param name="RecoveryCode">A single-use recovery code.</param>
public sealed record LoginTwoFactorRequest(string TwoFactorToken, string? Code, string? RecoveryCode);
