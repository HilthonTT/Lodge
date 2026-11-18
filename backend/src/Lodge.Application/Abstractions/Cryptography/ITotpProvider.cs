namespace Lodge.Application.Abstractions.Cryptography;

/// <summary>
/// Time-based one-time passwords (RFC 6238: HMAC-SHA1, 6 digits, 30-second steps).
/// </summary>
public interface ITotpProvider
{
    /// <summary>
    /// Generates a new random 160-bit secret, Base32-encoded.
    /// </summary>
    string GenerateSecret();

    /// <summary>
    /// Builds the <c>otpauth://totp/...</c> URI that authenticator apps read from a QR code.
    /// </summary>
    string GetOtpAuthUri(string secret, string accountName);

    /// <summary>
    /// Verifies a code with a tolerance of one time step in either direction.
    /// </summary>
    /// <returns>The matched time step, or null when the code is not valid.</returns>
    long? Verify(string secret, string? code, DateTime utcNow);
}
