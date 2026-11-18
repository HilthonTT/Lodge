using System.Text;
using Lodge.Infrastructure.Cryptography;

namespace Lodge.Application.UnitTests.Cryptography;

public class TotpProviderTests
{
    // RFC 6238, Appendix B: the SHA-1 seed is the ASCII string "12345678901234567890".
    private static readonly byte[] RfcKey = Encoding.ASCII.GetBytes("12345678901234567890");

    private readonly TotpProvider _provider = new();

    [Theory]
    [InlineData(59L, "94287082")]
    [InlineData(1111111109L, "07081804")]
    [InlineData(1111111111L, "14050471")]
    [InlineData(1234567890L, "89005924")]
    [InlineData(2000000000L, "69279037")]
    [InlineData(20000000000L, "65353130")]
    public void ComputeCode_Should_MatchTheRfc6238TestVectors(long unixTime, string expected)
    {
        TotpProvider.ComputeCode(RfcKey, unixTime / 30, 8).Should().Be(expected);
    }

    [Fact]
    public void Verify_Should_AcceptTheCurrentAndAdjacentSteps_AndRejectOthers()
    {
        string secret = Base32.Encode(RfcKey);
        DateTime utcNow = DateTimeOffset.FromUnixTimeSeconds(1111111111).UtcDateTime;
        long step = TotpProvider.GetTimeStep(utcNow);

        string Code(long s) => TotpProvider.ComputeCode(RfcKey, s, 6);

        _provider.Verify(secret, Code(step), utcNow).Should().Be(step);
        _provider.Verify(secret, Code(step - 1), utcNow).Should().Be(step - 1);
        _provider.Verify(secret, Code(step + 1), utcNow).Should().Be(step + 1);
        _provider.Verify(secret, Code(step - 2), utcNow).Should().BeNull();
        _provider.Verify(secret, Code(step + 2), utcNow).Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    public void Verify_Should_RejectMalformedCodes(string? code)
    {
        _provider.Verify(_provider.GenerateSecret(), code, DateTime.UtcNow).Should().BeNull();
    }

    [Fact]
    public void GenerateSecret_Should_Return160BitBase32Secrets()
    {
        string secret = _provider.GenerateSecret();

        secret.Should().MatchRegex("^[A-Z2-7]{32}$");
        Base32.TryDecode(secret, out byte[] bytes).Should().BeTrue();
        bytes.Should().HaveCount(20);
        _provider.GenerateSecret().Should().NotBe(secret);
    }

    [Fact]
    public void GetOtpAuthUri_Should_ContainIssuerSecretAndParameters()
    {
        string uri = _provider.GetOtpAuthUri("JBSWY3DPEHPK3PXP", "jane@lodge.dev");

        uri.Should().Be(
            "otpauth://totp/Lodge%3Ajane%40lodge.dev?secret=JBSWY3DPEHPK3PXP&issuer=Lodge&algorithm=SHA1&digits=6&period=30");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("f", "MY")]
    [InlineData("fo", "MZXQ")]
    [InlineData("foo", "MZXW6")]
    [InlineData("foob", "MZXW6YQ")]
    [InlineData("fooba", "MZXW6YTB")]
    [InlineData("foobar", "MZXW6YTBOI")]
    public void Base32_Should_MatchTheRfc4648TestVectors(string plain, string encoded)
    {
        Base32.Encode(Encoding.ASCII.GetBytes(plain)).Should().Be(encoded);

        if (plain.Length > 0)
        {
            Base32.TryDecode(encoded.ToLowerInvariant() + "====", out byte[] decoded).Should().BeTrue();
            Encoding.ASCII.GetString(decoded).Should().Be(plain);
        }
    }
}
