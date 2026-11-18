using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Extensions;
using Lodge.Contracts.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Lodge.Api.FunctionalTests.Abstractions;

[Collection(nameof(FunctionalTestCollection))]
public abstract class BaseFunctionalTest(FunctionalTestWebAppFactory factory)
{
    protected const string DemoEmail = "demo@lodge.dev";
    protected const string DemoPassword = "Demo123!";
    protected const string AdminEmail = "admin@lodge.dev";
    protected const string AdminPassword = "Admin123!";
    protected const string Password = "AbC-123";

    protected HttpClient HttpClient { get; } = factory.CreateClient();

    protected FunctionalTestWebAppFactory Factory { get; } = factory;

    /// <summary>
    /// Registers a new user and returns an authenticated client plus the user's id.
    /// </summary>
    protected async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedUserAsync()
    {
        var request = new RegisterRequest("First", "Last", $"{Guid.NewGuid():N}@lodge.test", Password);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/v1/authentication/register", request);
        response.EnsureSuccessStatusCode();

        return Authenticate(await response.GetTokenAsync());
    }

    protected Task<(HttpClient Client, Guid UserId)> LoginAsDemoUserAsync() => LoginAsync(DemoEmail, DemoPassword);

    protected Task<(HttpClient Client, Guid UserId)> LoginAsAdminAsync() => LoginAsync(AdminEmail, AdminPassword);

    protected async Task<(HttpClient Client, Guid UserId)> LoginAsync(string email, string password)
    {
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/login",
            new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        return Authenticate(await response.GetTokenAsync());
    }

    protected (HttpClient Client, Guid UserId) Authenticate(TokenResponse token)
    {
        HttpClient client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        Guid userId = Guid.Parse(new JsonWebToken(token.Token).GetClaim("userId").Value);

        return (client, userId);
    }
}
