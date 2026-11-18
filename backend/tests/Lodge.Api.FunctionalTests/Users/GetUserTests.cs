using Api.FunctionalTests.Abstractions;
using Api.FunctionalTests.Extensions;
using FluentAssertions;
using Lodge.Contracts.Authentication;
using Lodge.Contracts.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Api.FunctionalTests.Users;

public class GetUserTests : BaseFunctionalTest
{
    public GetUserTests(FunctionalTestWebAppFactory factory) 
        : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"api/v1/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_ReturnUser_WhenUserExists()
    {
        // Arrange
        Guid userId = await CreateUserAsync();

        // Act
        UserResponse? user = await HttpClient.GetFromJsonAsync<UserResponse>($"api/v1/users/{userId}");

        // Assert
        user.Should().NotBeNull();
    }

    private async Task<Guid> CreateUserAsync()
    {
        var request = new RegisterRequest("FirstName", "LastName", "email@email.com", "AbC-123");

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/v1/authentication/register", request);

        TokenResponse token = await response.GetTokenAsync();

        var tokenHandler = new JwtSecurityTokenHandler();
        JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token.Token);

        Claim? userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId");

        bool isGuid = Guid.TryParse(userIdClaim?.Value, out Guid userId);

        return userId;
    }
}
