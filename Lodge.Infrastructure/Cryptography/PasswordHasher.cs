using Lodge.Application.Abstractions.Cryptography;
using Lodge.Domain.Core.Guards;
using Lodge.Domain.Users;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace Lodge.Infrastructure.Cryptography;

/// <summary>
/// Represents the password hasher, used for hashing passwords and verifying hashed passwords.
/// </summary>
internal sealed class PasswordHasher : IPasswordHasher, IPasswordHashChecker
{
    private const KeyDerivationPrf Prf = KeyDerivationPrf.HMACSHA256;
    private const int IterationCount = 10000;
    private const int NumberOfBytesRequested = 256 / 8;
    private const int SaltSize = 128 / 8;

    /// <inheritdoc />
    public string HashPassword(Password? password)
    {
        Ensure.NotNull(password, nameof(password));

        string hashedPassword = Convert.ToBase64String(HashPasswordInternal(password.Value));

        return hashedPassword;
    }

    /// <inheritdoc />
    public bool HashesMatch(string? passwordHash, string? providedPassword)
    {
        Ensure.NotNullOrEmpty(passwordHash, nameof(passwordHash));
        Ensure.NotNullOrEmpty(providedPassword, nameof(providedPassword));

        byte[] decodedHashedPassword = Convert.FromBase64String(passwordHash);

        if (decodedHashedPassword.Length == 0)
        {
            return false;
        }

        bool verified = VerifyPasswordHashInternal(decodedHashedPassword, providedPassword);

        return verified;
    }

    /// <summary>
    /// Returns the bytes of the hash for the specified password.
    /// </summary>
    /// <param name="password">The password to be hashed.</param>
    /// <returns>The bytes of the hash for the specified password.</returns>
    private byte[] HashPasswordInternal(string password)
    {
        byte[] salt = GetRandomSalt();

        byte[] subKey = KeyDerivation.Pbkdf2(password, salt, Prf, IterationCount, NumberOfBytesRequested);

        byte[] outputBytes = new byte[salt.Length + subKey.Length];

        Buffer.BlockCopy(salt, 0, outputBytes, 0, salt.Length);

        Buffer.BlockCopy(subKey, 0, outputBytes, salt.Length, subKey.Length);

        return outputBytes;
    }

    /// <summary>
    /// Gets a randomly generated salt.
    /// </summary>
    /// <returns>The randomly generated salt.</returns>
    private static byte[] GetRandomSalt()
    {
        byte[] salt = new byte[SaltSize];

        RandomNumberGenerator.Fill(salt);

        return salt;
    }

    /// <summary>
    /// Verifies the bytes of the hashed password with the specified password.
    /// </summary>
    /// <param name="hashedPassword">The bytes of the hashed password.</param>
    /// <param name="password">The password to verify with.</param>
    /// <returns>True if the hashes match, otherwise false.</returns>
    private static bool VerifyPasswordHashInternal(byte[] hashedPassword, string password)
    {
        try
        {
            byte[] salt = new byte[SaltSize];

            Buffer.BlockCopy(hashedPassword, 0, salt, 0, salt.Length);

            int subKeyLength = hashedPassword.Length - salt.Length;

            if (subKeyLength < SaltSize)
            {
                return false;
            }

            byte[] expectedSubKey = new byte[subKeyLength];

            Buffer.BlockCopy(hashedPassword, salt.Length, expectedSubKey, 0, expectedSubKey.Length);

            byte[] actualSubKey = KeyDerivation.Pbkdf2(password, salt, Prf, IterationCount, subKeyLength);

            return ByteArraysEqual(actualSubKey, expectedSubKey);
        }
        catch
        {
            return false;
        }
    }
    /// <summary>
    /// Returns true if the specified byte arrays are equal, otherwise false.
    /// </summary>
    /// <param name="a">The first byte array.</param>
    /// <param name="b">The second byte array.</param>
    /// <returns>True if the arrays are equal, otherwise false.</returns>
    private static bool ByteArraysEqual(byte[] a, byte[] b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null || a.Length != b.Length)
        {
            return false;
        }

        bool areSame = true;

        for (int i = 0; i < a.Length; i++)
        {
            areSame &= a[i] == b[i];
        }

        return areSame;
    }
}
