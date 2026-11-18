namespace Lodge.Contracts.Authentication;

/// <summary>
/// Requests a password reset e-mail.
/// </summary>
/// <param name="Email">The e-mail address of the account.</param>
public sealed record ForgotPasswordRequest(string Email);
