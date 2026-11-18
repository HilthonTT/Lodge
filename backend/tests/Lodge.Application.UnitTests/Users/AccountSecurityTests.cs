using Lodge.Application.Abstractions.Cryptography;
using Lodge.Application.Users.Avatar;

namespace Lodge.Application.UnitTests.Users;

public class AccountSecurityTests
{
    [Fact]
    public void UrlSafeTokens_Should_Have256BitsAndBeUrlSafe()
    {
        string token = SecureTokens.GenerateUrlSafeToken();

        token.Should().MatchRegex("^[A-Za-z0-9_-]{43}$");
        SecureTokens.GenerateUrlSafeToken().Should().NotBe(token);
        SecureTokens.Hash(token).Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void RecoveryCodes_Should_BeFormattedAndHashedInsensitively()
    {
        string code = SecureTokens.GenerateRecoveryCode();

        code.Should().MatchRegex("^[a-z2-9]{5}-[a-z2-9]{5}$");
        SecureTokens.HashRecoveryCode($" {code.ToUpperInvariant().Replace("-", string.Empty, StringComparison.Ordinal)} ")
            .Should().Be(SecureTokens.HashRecoveryCode(code));
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10 }, "image/jpeg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0 }, "image/png")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x45, 0x42, 0x50, 0x56, 0x50 }, "image/webp")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x41, 0x56, 0x45 }, null)]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, null)]
    [InlineData(new byte[] { 0x3C, 0x73, 0x76, 0x67 }, null)]
    [InlineData(new byte[0], null)]
    public void ImageFormats_Should_DetectByMagicBytes(byte[] header, string? expected)
    {
        ImageFormats.Detect(header).Should().Be(expected);
    }
}
