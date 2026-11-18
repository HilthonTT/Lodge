namespace Lodge.Contracts.Users;

/// <summary>
/// The TOTP secret to add to an authenticator app.
/// </summary>
/// <param name="Secret">The Base32-encoded secret (for manual entry).</param>
/// <param name="OtpauthUri">The <c>otpauth://</c> URI to render as a QR code.</param>
public sealed record TwoFactorSetupResponse(string Secret, string OtpauthUri);
