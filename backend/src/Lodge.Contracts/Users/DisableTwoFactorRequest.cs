namespace Lodge.Contracts.Users;

/// <summary>
/// Disables two-factor authentication.
/// </summary>
/// <param name="Password">The current password. Required unless the account has no password (social login only).</param>
/// <param name="Code">The 6-digit code of the authenticator app.</param>
public sealed record DisableTwoFactorRequest(string? Password, string Code);
