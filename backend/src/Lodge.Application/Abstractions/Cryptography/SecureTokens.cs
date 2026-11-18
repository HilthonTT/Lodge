using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Lodge.Application.Abstractions.Cryptography;

/// <summary>
/// Random tokens for e-mailed links and recovery codes, and their hashes.
/// </summary>
public static class SecureTokens
{
    public const int RecoveryCodeCount = 10;

    // No 0/o, 1/l/i: recovery codes are typed in by hand.
    private const string RecoveryCodeAlphabet = "abcdefghjkmnpqrstuvwxyz23456789";

    /// <summary>
    /// Generates a URL-safe (Base64Url, no padding) token from 32 random bytes.
    /// </summary>
    public static string GenerateUrlSafeToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Returns the lower-case hex SHA-256 hash of the value.
    /// </summary>
    public static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    /// <summary>
    /// Generates a recovery code formatted as <c>xxxxx-xxxxx</c>.
    /// </summary>
    public static string GenerateRecoveryCode()
    {
        var builder = new StringBuilder(11);

        for (int i = 0; i < 10; i++)
        {
            if (i == 5)
            {
                builder.Append('-');
            }

            builder.Append(RecoveryCodeAlphabet[RandomNumberGenerator.GetInt32(RecoveryCodeAlphabet.Length)]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Hashes a recovery code after normalizing it (case, dashes and whitespace are ignored).
    /// </summary>
    public static string HashRecoveryCode(string recoveryCode)
    {
        string normalized = new(recoveryCode
            .Where(c => !char.IsWhiteSpace(c) && c != '-')
            .Select(char.ToLowerInvariant)
            .ToArray());

        return Hash(normalized);
    }
}
