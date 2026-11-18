namespace Lodge.Contracts.Authentication;

/// <summary>
/// Verifies an e-mail address with the token from the verification e-mail.
/// </summary>
/// <param name="Token">The token.</param>
public sealed record VerifyEmailRequest(string Token);
