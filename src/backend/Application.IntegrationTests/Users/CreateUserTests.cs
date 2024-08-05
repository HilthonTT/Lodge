using Application.IntegrationTests.Abstractions;
using FluentAssertions;
using Lodge.Application.Users.Create;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Application.IntegrationTests.Users;

public class CreateUserTests : BaseIntegrationTest
{
    private const string Password = "AbC-123";

    public CreateUserTests(IntegrationTestWebFactory factory) 
        : base(factory)
    {
    }

    [Fact]
    public async Task Handle_Should_CreateUser_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateUserCommand(
            Faker.Internet.UserName(), 
            Faker.Internet.UserName(),
            Faker.Internet.Email(),
            Password);

        // Act
        Result<TokenResponse> result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_AddUserToDatabase_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateUserCommand(
            Faker.Internet.UserName(),
            Faker.Internet.UserName(),
            Faker.Internet.Email(),
            Password);

        // Act
        Result<TokenResponse> result = await Sender.Send(command);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(result.Value.Token);

        Claim? userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId");

        bool isGuid = Guid.TryParse(userIdClaim?.Value, out Guid userId);

        // Assert
        User? user = await LodgeDbContext.Users.FindAsync(userId);

        user.Should().NotBeNull();
    }
}
