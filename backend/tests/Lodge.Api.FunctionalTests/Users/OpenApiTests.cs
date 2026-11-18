using System.Net;
using System.Text.Json;
using Lodge.Api.FunctionalTests.Abstractions;

namespace Lodge.Api.FunctionalTests.Users;

public class OpenApiTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task OpenApiDocument_Should_DescribeTheAccountEndpoints()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string[] paths = document.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToArray();

        paths.Should().Contain(
        [
            "/api/v1/authentication/verify-email",
            "/api/v1/authentication/resend-verification",
            "/api/v1/authentication/forgot-password",
            "/api/v1/authentication/reset-password",
            "/api/v1/authentication/login/two-factor",
            "/api/v1/authentication/refresh-token",
            "/api/v1/authentication/providers",
            "/api/v1/authentication/external/{provider}",
            "/api/v1/authentication/external/{provider}/callback",
            "/api/v1/users/{userId}/two-factor/setup",
            "/api/v1/users/{userId}/two-factor/enable",
            "/api/v1/users/{userId}/two-factor/disable",
            "/api/v1/users/{userId}/two-factor/recovery-codes",
            "/api/v1/users/{userId}/avatar",
            "/api/v1/users/{userId}/profile",
        ]);
    }

    [Theory]
    [InlineData("/api/v1/bookings")]
    [InlineData("/api/v1/reviews")]
    [InlineData("/api/v1/files")]
    [InlineData("/api/v1/experience-bookings")]
    public async Task OpenApiDocument_Should_DescribeTheIdempotencyKeyAsARequiredGuid(string path)
    {
        using JsonDocument document = JsonDocument.Parse(await HttpClient.GetStringAsync("openapi/v1.json"));

        JsonElement header = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty("post")
            .GetProperty("parameters").EnumerateArray()
            .Single(p => p.GetProperty("name").GetString() == "X-Idempotency-Key");

        header.GetProperty("in").GetString().Should().Be("header");
        header.GetProperty("required").GetBoolean().Should().BeTrue();
        header.GetProperty("schema").GetProperty("format").GetString().Should().Be("uuid");
    }

    [Fact]
    public async Task OpenApiDocument_Should_DescribeTheHostingAndListingEndpoints()
    {
        using JsonDocument document = JsonDocument.Parse(await HttpClient.GetStringAsync("openapi/v1.json"));
        string[] paths = document.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToArray();

        paths.Should().Contain(
        [
            "/api/v1/hosting/photos",
            "/api/v1/hosting/listings",
            "/api/v1/hosting/listings/{apartmentId}",
            "/api/v1/hosting/listings/{apartmentId}/status",
            "/api/v1/hosting/listings/{apartmentId}/calendar",
            "/api/v1/hosting/bookings",
            "/api/v1/hosting/bookings/{bookingId}/accept",
            "/api/v1/hosting/bookings/{bookingId}/decline",
            "/api/v1/hosting/summary",
            "/api/v1/apartments/{apartmentId}/calendar",
            "/api/v1/apartments/price-range",
            "/api/v1/locations/suggest",
            "/api/v1/bookings/{bookingId}/dates",
            "/api/v1/wishlists",
            "/api/v1/wishlists/saved-apartment-ids",
            "/api/v1/wishlists/{wishlistId}",
            "/api/v1/wishlists/{wishlistId}/items/{apartmentId}",
        ]);
    }
}
