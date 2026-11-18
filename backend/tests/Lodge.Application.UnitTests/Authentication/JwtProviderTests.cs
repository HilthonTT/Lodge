using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Lodge.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;

namespace Lodge.Application.UnitTests.Authentication;

public class JwtProviderTests
{
    private readonly JwtProvider _provider;

    public JwtProviderTests()
    {
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        _provider = new JwtProvider(dateTimeProvider, Options.Create(new JwtOptions
        {
            Issuer = "Lodge",
            Audience = "lodge.dev",
            SecurityKey = "unit-test-signing-key-0123456789-abcdef",
        }));
    }

    [Theory]
    [InlineData(UserRole.Guest, "Guest")]
    [InlineData(UserRole.Admin, "Admin")]
    public void Create_Should_IssueTheRoleClaim(UserRole role, string expected)
    {
        var user = User.Create(
            FirstName.Create("First").Value,
            LastName.Create("Last").Value,
            Email.Create("user@lodge.dev").Value,
            "hash");
        user.ChangeRole(role);

        var token = new JsonWebToken(_provider.Create(user));

        token.GetClaim("role").Value.Should().Be(expected);
        token.GetClaim("userId").Value.Should().Be(user.Id.ToString());
    }
}
