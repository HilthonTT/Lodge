using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Authentication;

namespace Lodge.Application.Authentication.LoginTwoFactor;

/// <summary>
/// Exchanges a two-factor challenge token plus a TOTP code or a recovery code for an access token.
/// </summary>
public sealed record LoginTwoFactorCommand(string TwoFactorToken, string? Code, string? RecoveryCode)
    : ICommand<TokenResponse>;
