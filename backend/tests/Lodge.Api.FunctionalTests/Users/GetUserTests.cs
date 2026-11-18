using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Contracts.Users;

namespace Lodge.Api.FunctionalTests.Users;

public class GetUserTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Should_ReturnUnauthorized_WhenAnonymous()
    {
        HttpResponseMessage response = await HttpClient.GetAsync($"api/v1/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_ReturnOwnProfile()
    {
        (HttpClient client, Guid userId) = await CreateAuthenticatedUserAsync();

        UserResponse? user = await client.GetFromJsonAsync<UserResponse>($"api/v1/users/{userId}");

        user.Should().NotBeNull();
        user!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task Should_ReturnForbidden_ForAnotherUsersProfile()
    {
        // Regression: profiles (including e-mail addresses) were readable anonymously by id.
        (HttpClient client, _) = await CreateAuthenticatedUserAsync();
        (_, Guid otherUserId) = await CreateAuthenticatedUserAsync();

        HttpResponseMessage response = await client.GetAsync($"api/v1/users/{otherUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
