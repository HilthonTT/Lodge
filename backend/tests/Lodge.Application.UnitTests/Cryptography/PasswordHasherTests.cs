using Lodge.Domain.Users;
using Lodge.Infrastructure.Cryptography;

namespace Lodge.Application.UnitTests.Cryptography;

public class PasswordHasherTests
{
    private const string PlainPassword = "C0rrect-Horse";

    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashesMatch_Should_ReturnTrue_ForTheHashedPassword()
    {
        string hash = _hasher.HashPassword(Password.Create(PlainPassword).Value);

        _hasher.HashesMatch(hash, PlainPassword).Should().BeTrue();
    }

    [Fact]
    public void HashesMatch_Should_ReturnFalse_ForAWrongPassword()
    {
        string hash = _hasher.HashPassword(Password.Create(PlainPassword).Value);

        _hasher.HashesMatch(hash, "Wr0ng-Horse").Should().BeFalse();
        _hasher.HashesMatch(hash, PlainPassword.ToUpperInvariant()).Should().BeFalse();
    }

    [Fact]
    public void HashPassword_Should_UseTheSelfDescribingFormat_WithDefaultParameters()
    {
        string hash = _hasher.HashPassword(Password.Create(PlainPassword).Value);

        string[] parts = hash.Split('$');
        parts.Should().HaveCount(4);
        parts[0].Should().Be("pbkdf2-sha512");
        parts[1].Should().Be("210000");
        Convert.FromBase64String(parts[2]).Should().HaveCount(16);
        Convert.FromBase64String(parts[3]).Should().HaveCount(32);

        PasswordHasher.TryParse(hash, out int iterations, out byte[] salt, out byte[] subkey).Should().BeTrue();
        iterations.Should().Be(PasswordHasher.DefaultIterations);
        salt.Should().Equal(Convert.FromBase64String(parts[2]));
        subkey.Should().Equal(Convert.FromBase64String(parts[3]));
    }

    [Fact]
    public void HashPassword_Should_UseARandomSalt()
    {
        Password password = Password.Create(PlainPassword).Value;

        _hasher.HashPassword(password).Should().NotBe(_hasher.HashPassword(password));
    }

    [Theory]
    [InlineData(1_000)]
    [InlineData(100_000)]
    [InlineData(300_000)]
    public void HashesMatch_Should_VerifyHashes_CreatedWithAnotherIterationCount(int iterations)
    {
        string hash = PasswordHasher.Hash(PlainPassword, iterations);

        hash.Should().StartWith($"pbkdf2-sha512${iterations}$");
        _hasher.HashesMatch(hash, PlainPassword).Should().BeTrue();
        _hasher.HashesMatch(hash, "Wr0ng-Horse").Should().BeFalse();
    }

    [Fact]
    public void HashesMatch_Should_ReturnFalse_WhenTheIterationCountWasTamperedWith()
    {
        string hash = PasswordHasher.Hash(PlainPassword, 1_000);
        string tampered = hash.Replace("$1000$", "$1001$", StringComparison.Ordinal);

        _hasher.HashesMatch(tampered, PlainPassword).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("pbkdf2-sha512$210000$c2FsdA==")]
    [InlineData("pbkdf2-sha256$1000$c2FsdHNhbHRzYWx0c2FsdA==$aGFzaGhhc2hoYXNoaGFzaA==")]
    [InlineData("pbkdf2-sha512$abc$c2FsdHNhbHRzYWx0c2FsdA==$aGFzaGhhc2hoYXNoaGFzaA==")]
    [InlineData("pbkdf2-sha512$-5$c2FsdHNhbHRzYWx0c2FsdA==$aGFzaGhhc2hoYXNoaGFzaA==")]
    [InlineData("pbkdf2-sha512$0$c2FsdHNhbHRzYWx0c2FsdA==$aGFzaGhhc2hoYXNoaGFzaA==")]
    [InlineData("pbkdf2-sha512$99999999999$c2FsdHNhbHRzYWx0c2FsdA==$aGFzaGhhc2hoYXNoaGFzaA==")]
    [InlineData("pbkdf2-sha512$1000$not base64!$aGFzaGhhc2hoYXNoaGFzaA==")]
    [InlineData("pbkdf2-sha512$1000$c2FsdHNhbHRzYWx0c2FsdA==$")]
    [InlineData("pbkdf2-sha512$1000$$aGFzaGhhc2hoYXNoaGFzaA==")]
    [InlineData("pbkdf2-sha512$1000$c2FsdA==$aGFzaA==$extra")]
    public void HashesMatch_Should_ReturnFalse_ForMalformedHashes(string? hash)
    {
        bool matches = true;

        Action act = () => matches = _hasher.HashesMatch(hash, PlainPassword);

        act.Should().NotThrow();
        matches.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HashesMatch_Should_ReturnFalse_ForAnEmptyPassword(string? providedPassword)
    {
        string hash = PasswordHasher.Hash(PlainPassword, 1_000);

        _hasher.HashesMatch(hash, providedPassword).Should().BeFalse();
    }

    [Fact]
    public void VerifyPasswordHash_Should_DetectTheCurrentPassword_ForChangePassword()
    {
        // ChangePassword relies on this to reject "new password equals the current one".
        var user = User.Create(
            FirstName.Create("First").Value,
            LastName.Create("Last").Value,
            Email.Create("user@lodge.dev").Value,
            _hasher.HashPassword(Password.Create(PlainPassword).Value));

        user.VerifyPasswordHash(PlainPassword, _hasher).Should().BeTrue();
        user.VerifyPasswordHash("An0ther-Horse", _hasher).Should().BeFalse();
    }
}
