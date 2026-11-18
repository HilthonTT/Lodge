namespace Lodge.Contracts.Authentication;

/// <summary>
/// Sets a new password with the token from the password reset e-mail.
/// </summary>
/// <param name="Token">The token.</param>
/// <param name="Password">The new password.</param>
public sealed record ResetPasswordRequest(string Token, string Password);
