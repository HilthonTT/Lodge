using Lodge.Application.Abstractions.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Lodge.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;

namespace Lodge.Application.UnitTests.Authentication;

public class TwoFactorChallengeTests
{
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly JwtProvider _provider;
    private readonly User _user = User.Create(
        FirstName.Create("First").Value,
        LastName.Create("Last").Value,
        Email.Create("user@lodge.dev").Value,
        "hash");

    public TwoFactorChallengeTests()
    {
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _provider = new JwtProvider(_dateTimeProvider, Options.Create(new JwtOptions
        {
            Issuer = "Lodge",
            Audience = "lodge.dev",
            SecurityKey = "unit-test-signing-key-0123456789-abcdef",
        }));
    }

    [Fact]
    public async Task Challenge_Should_RoundTrip()
    {
        string token = _provider.CreateTwoFactorChallenge(_user);

        TwoFactorChallenge? challenge = await _provider.ValidateTwoFactorChallengeAsync(token);

        challenge.Should().NotBeNull();
        challenge!.UserId.Should().Be(_user.Id);
        challenge.TokenId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Challenge_Should_Expire_AfterFiveMinutes()
    {
        string token = _provider.CreateTwoFactorChallenge(_user);

        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow.AddMinutes(5).AddSeconds(1));

        (await _provider.ValidateTwoFactorChallengeAsync(token)).Should().BeNull();
    }

    [Fact]
    public async Task AccessToken_Should_NotBeAcceptedAsChallenge()
    {
        string accessToken = _provider.Create(_user);

        (await _provider.ValidateTwoFactorChallengeAsync(accessToken)).Should().BeNull();
    }

    [Fact]
    public void Challenge_Should_UseItsOwnAudience()
    {
        var token = new JsonWebToken(_provider.CreateTwoFactorChallenge(_user));

        token.Audiences.Should().ContainSingle().Which.Should().NotBe("lodge.dev");
        token.GetClaim("purpose").Value.Should().Be("two-factor");
    }

    [Theory]
    [InlineData(false, "false")]
    [InlineData(true, "true")]
    public void AccessToken_Should_CarryTheEmailVerifiedClaim(bool verified, string expected)
    {
        if (verified)
        {
            _user.VerifyEmail(DateTime.UtcNow);
        }

        var token = new JsonWebToken(_provider.Create(_user));

        token.GetClaim("emailVerified").Value.Should().Be(expected);
    }
}
