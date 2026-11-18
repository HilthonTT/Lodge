namespace Lodge.Application.Abstractions.Cryptography;

/// <summary>
/// Encrypts secrets that are stored in the database (e.g. TOTP secrets).
/// </summary>
public interface ISecretProtector
{
    string Protect(string plaintext);

    /// <summary>
    /// Decrypts a value produced by <see cref="Protect"/>. Returns null when it cannot be decrypted.
    /// </summary>
    string? Unprotect(string protectedValue);
}
