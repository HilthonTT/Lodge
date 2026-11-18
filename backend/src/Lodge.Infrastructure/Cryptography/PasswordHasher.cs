using System.Globalization;
using System.Security.Cryptography;
using Lodge.Application.Abstractions.Cryptography;
using Lodge.Domain.Core.Guards;
using Lodge.Domain.Users;

namespace Lodge.Infrastructure.Cryptography;

/// <summary>
/// Hashes passwords with PBKDF2-HMAC-SHA512 and verifies them.
/// </summary>
/// <remarks>
/// Hashes are self-describing: <c>pbkdf2-sha512$&lt;iterations&gt;$&lt;base64 salt&gt;$&lt;base64 subkey&gt;</c>.
/// Verification reads the iteration count from the stored hash, so <see cref="DefaultIterations"/> can be raised
/// later without invalidating existing hashes.
/// </remarks>
internal sealed class PasswordHasher : IPasswordHasher, IPasswordHashChecker
{
    internal const string Algorithm = "pbkdf2-sha512";

    /// <summary>
    /// The OWASP-recommended iteration count for PBKDF2-HMAC-SHA512.
    /// </summary>
    internal const int DefaultIterations = 210_000;

    internal const int SaltSize = 16;

    internal const int SubkeySize = 32;

    private const char Separator = '$';

    // Upper bound for stored iteration counts, so a tampered hash cannot make verification arbitrarily slow.
    private const int MaxIterations = 10_000_000;

    private static readonly HashAlgorithmName Prf = HashAlgorithmName.SHA512;

    /// <inheritdoc />
    public string HashPassword(Password? password)
    {
        Ensure.NotNull(password);

        return Hash(password.Value, DefaultIterations);
    }

    /// <inheritdoc />
    public bool HashesMatch(string? passwordHash, string? providedPassword)
    {
        if (string.IsNullOrEmpty(passwordHash) || string.IsNullOrEmpty(providedPassword))
        {
            return false;
        }

        if (!TryParse(passwordHash, out int iterations, out byte[] salt, out byte[] expectedSubkey))
        {
            return false;
        }

        byte[] actualSubkey = Rfc2898DeriveBytes.Pbkdf2(providedPassword, salt, iterations, Prf, expectedSubkey.Length);

        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }

    /// <summary>
    /// Hashes the password with the specified iteration count.
    /// </summary>
    internal static string Hash(string password, int iterations)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Prf, SubkeySize);

        return string.Join(
            Separator,
            Algorithm,
            iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(subkey));
    }

    /// <summary>
    /// Parses a stored hash. Returns false for anything that is not a well-formed hash of this scheme.
    /// </summary>
    internal static bool TryParse(string passwordHash, out int iterations, out byte[] salt, out byte[] subkey)
    {
        iterations = 0;
        salt = [];
        subkey = [];

        string[] parts = passwordHash.Split(Separator);

        if (parts.Length != 4 || !string.Equals(parts[0], Algorithm, StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations) ||
            iterations is < 1 or > MaxIterations)
        {
            iterations = 0;
            return false;
        }

        if (!TryFromBase64(parts[2], out salt) || !TryFromBase64(parts[3], out subkey))
        {
            return false;
        }

        return salt.Length > 0 && subkey.Length > 0;
    }

    private static bool TryFromBase64(string value, out byte[] bytes)
    {
        var buffer = new byte[(value.Length * 3 / 4) + 3];

        if (value.Length == 0 || !Convert.TryFromBase64String(value, buffer, out int written))
        {
            bytes = [];
            return false;
        }

        bytes = buffer[..written];
        return true;
    }
}
