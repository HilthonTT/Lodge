namespace Lodge.Contracts.Authentication;

/// <summary>
/// The result of a password login. When the user has two-factor authentication enabled, <see cref="Token"/> is
/// null, <see cref="RequiresTwoFactor"/> is true and <see cref="TwoFactorToken"/> must be exchanged for an access
/// token at <c>POST /authentication/login/two-factor</c>.
/// </summary>
/// <param name="Token">The access token, or null when a second factor is required.</param>
/// <param name="RequiresTwoFactor">True when a second factor is required.</param>
/// <param name="TwoFactorToken">The short-lived (5 minute) two-factor challenge token, or null.</param>
public sealed record LoginResponse(string? Token, bool RequiresTwoFactor, string? TwoFactorToken)
{
    public static LoginResponse WithToken(string token) => new(token, false, null);

    public static LoginResponse WithTwoFactorChallenge(string twoFactorToken) => new(null, true, twoFactorToken);
}
