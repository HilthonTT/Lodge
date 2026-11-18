namespace Lodge.Application.Authentication.External;

/// <summary>
/// The outcome of a successful social login: either an access token or (with 2FA) a two-factor challenge token.
/// </summary>
public sealed record ExternalLoginResult(string? Token, string? TwoFactorToken, string ReturnUrl);
