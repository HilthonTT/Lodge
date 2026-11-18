namespace Lodge.Contracts.Users;

/// <summary>
/// A request that carries a code from the authenticator app.
/// </summary>
/// <param name="Code">The 6-digit code.</param>
public sealed record TwoFactorCodeRequest(string Code);
