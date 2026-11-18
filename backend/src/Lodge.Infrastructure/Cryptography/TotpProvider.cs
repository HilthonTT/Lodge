using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using Lodge.Application.Abstractions.Cryptography;

namespace Lodge.Infrastructure.Cryptography;

/// <summary>
/// RFC 6238 TOTP with the parameters every authenticator app supports: HMAC-SHA1, 6 digits, 30-second steps.
/// Codes of the previous and next step are accepted to tolerate clock drift.
/// </summary>
internal sealed class TotpProvider : ITotpProvider
{
    internal const string Issuer = "Lodge";
    internal const int Digits = 6;
    internal const int StepSeconds = 30;
    internal const int SecretSizeInBytes = 20;
    internal const int AllowedDriftSteps = 1;

    public string GenerateSecret() => Base32.Encode(RandomNumberGenerator.GetBytes(SecretSizeInBytes));

    public string GetOtpAuthUri(string secret, string accountName)
    {
        string label = Uri.EscapeDataString($"{Issuer}:{accountName}");

        return $"otpauth://totp/{label}?secret={secret}&issuer={Uri.EscapeDataString(Issuer)}" +
               $"&algorithm=SHA1&digits={Digits}&period={StepSeconds}";
    }

    public long? Verify(string secret, string? code, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        string normalized = code.Replace(" ", string.Empty, StringComparison.Ordinal);

        if (normalized.Length != Digits || !normalized.All(char.IsAsciiDigit) || !Base32.TryDecode(secret, out byte[] key))
        {
            return null;
        }

        long currentStep = GetTimeStep(utcNow);

        for (long step = currentStep - AllowedDriftSteps; step <= currentStep + AllowedDriftSteps; step++)
        {
            string expected = ComputeCode(key, step, Digits);

            if (CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.ASCII.GetBytes(expected),
                    System.Text.Encoding.ASCII.GetBytes(normalized)))
            {
                return step;
            }
        }

        return null;
    }

    internal static long GetTimeStep(DateTime utcNow) =>
        new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc)).ToUnixTimeSeconds() / StepSeconds;

    /// <summary>
    /// Computes the HOTP value (RFC 4226) for the time step, with dynamic truncation.
    /// </summary>
    internal static string ComputeCode(byte[] key, long step, int digits)
    {
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);

        Span<byte> hash = stackalloc byte[HMACSHA1.HashSizeInBytes];
        HMACSHA1.HashData(key, counter, hash);

        int offset = hash[^1] & 0x0F;
        int binary = ((hash[offset] & 0x7F) << 24) |
                     (hash[offset + 1] << 16) |
                     (hash[offset + 2] << 8) |
                     hash[offset + 3];

        int otp = binary % (int)Math.Pow(10, digits);

        return otp.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
    }
}
