using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Api.FunctionalTests.Contracts;
using Lodge.Api.FunctionalTests.Extensions;
using Lodge.Contracts.Users;

namespace Lodge.Api.FunctionalTests.Users;

public class ProfileTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52];

    [Fact]
    public async Task PublicProfile_Should_BeAnonymous_AndHideTheEmailAndLastName()
    {
        (_, Guid demoUserId) = await LoginAsDemoUserAsync();

        HttpResponseMessage response = await HttpClient.GetAsync($"api/v1/users/{demoUserId}/profile");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement profile = document.RootElement;

        profile.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            "id", "firstName", "lastInitial", "avatarUrl", "bio", "joinedOnUtc", "emailVerified", "reviewsWrittenCount",
            "isSuperhost", "hostingSince", "listings", "hostReviewCount", "hostAverageRating");
        profile.GetProperty("firstName").GetString().Should().Be("Alex");
        profile.GetProperty("lastInitial").GetString().Should().Be("M");
        profile.GetProperty("bio").GetString().Should().NotBeNullOrWhiteSpace();
        profile.GetProperty("emailVerified").GetBoolean().Should().BeTrue();
        profile.GetProperty("reviewsWrittenCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task PublicProfile_Should_ReturnNotFound_ForUnknownUsers()
    {
        (await HttpClient.GetAsync($"api/v1/users/{Guid.NewGuid()}/profile")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Should_ChangeTheBio()
    {
        (HttpClient client, Guid userId) = await CreateAuthenticatedUserAsync();

        (await client.PutAsJsonAsync($"api/v1/users/{userId}", new UpdateUserRequest("First", "Last", null, "  Hello there  ")))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<UserResponse>("api/v1/users/auth"))!.Bio.Should().Be("Hello there");

        // Null leaves the bio unchanged.
        (await client.PutAsJsonAsync($"api/v1/users/{userId}", new UpdateUserRequest("First", "Last", null)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await HttpClient.GetFromJsonAsync<UserProfileResponse>($"api/v1/users/{userId}/profile"))!.Bio.Should().Be("Hello there");

        HttpResponseMessage tooLong = await client.PutAsJsonAsync(
            $"api/v1/users/{userId}", new UpdateUserRequest("First", "Last", null, new string('a', 501)));
        tooLong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CustomProblemDetails problem = await tooLong.GetProblemDetailsAsync();
        problem.Errors.Select(e => e.Code).Should().Contain("User.BioTooLong");

        (await client.PutAsJsonAsync($"api/v1/users/{userId}", new UpdateUserRequest("First", "Last", null, "")))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<UserResponse>("api/v1/users/auth"))!.Bio.Should().BeNull();
    }

    [Fact]
    public async Task Avatar_Should_BeSetReplacedAndRemoved()
    {
        (HttpClient client, Guid userId) = await CreateAuthenticatedUserAsync();

        HttpResponseMessage first = await UploadAsync(client, userId, Png, "image/png");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        string firstUrl = (await first.Content.ReadFromJsonAsync<AvatarResponse>())!.AvatarUrl;
        firstUrl.Should().MatchRegex($"^{FunctionalTestWebAppFactory.PublicBaseUrl}/api/v1/files/[0-9a-f-]{{36}}$");

        UserResponse? user = await client.GetFromJsonAsync<UserResponse>("api/v1/users/auth");
        user!.AvatarUrl.Should().Be(firstUrl);
        (await HttpClient.GetFromJsonAsync<UserProfileResponse>($"api/v1/users/{userId}/profile"))!.AvatarUrl.Should().Be(firstUrl);

        // Replacing deletes the previous file.
        HttpResponseMessage second = await UploadAsync(client, userId, Png, "image/png");
        string secondUrl = (await second.Content.ReadFromJsonAsync<AvatarResponse>())!.AvatarUrl;
        Factory.Blobs.Contains(FileId(firstUrl)).Should().BeFalse();
        Factory.Blobs.Contains(FileId(secondUrl)).Should().BeTrue();

        (await client.DeleteAsync($"api/v1/users/{userId}/avatar")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<UserResponse>("api/v1/users/auth"))!.AvatarUrl.Should().BeNull();
        Factory.Blobs.Contains(FileId(secondUrl)).Should().BeFalse();
    }

    [Theory]
    [InlineData("image/png", "not really a png")]
    [InlineData("image/gif", "GIF89a....")]
    [InlineData("text/plain", "hello")]
    public async Task Avatar_Should_RejectFilesThatAreNotJpegPngOrWebp(string contentType, string content)
    {
        (HttpClient client, Guid userId) = await CreateAuthenticatedUserAsync();

        HttpResponseMessage response = await UploadAsync(client, userId, System.Text.Encoding.ASCII.GetBytes(content), contentType);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CustomProblemDetails problem = await response.GetProblemDetailsAsync();
        (problem.Title == "User.InvalidAvatar" || problem.Errors.Any(e => e.Code == "User.InvalidAvatar")).Should().BeTrue();
    }

    [Fact]
    public async Task Avatar_Should_RejectFilesOverFiveMegabytes()
    {
        (HttpClient client, Guid userId) = await CreateAuthenticatedUserAsync();
        byte[] large = new byte[(5 * 1024 * 1024) + 1];
        Png.CopyTo(large, 0);

        HttpResponseMessage response = await UploadAsync(client, userId, large, "image/png");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.GetProblemDetailsAsync()).Errors.Select(e => e.Code).Should().Contain("User.AvatarTooLarge");
    }

    [Fact]
    public async Task Avatar_Should_BeForbidden_ForAnotherUser()
    {
        (HttpClient client, _) = await CreateAuthenticatedUserAsync();
        (_, Guid otherUserId) = await CreateAuthenticatedUserAsync();

        (await UploadAsync(client, otherUserId, Png, "image/png")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static Guid FileId(string avatarUrl) => Guid.Parse(avatarUrl[^36..]);

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, Guid userId, byte[] bytes, string contentType)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", "avatar.bin");

        return await client.PostAsync($"api/v1/users/{userId}/avatar", content);
    }
}
