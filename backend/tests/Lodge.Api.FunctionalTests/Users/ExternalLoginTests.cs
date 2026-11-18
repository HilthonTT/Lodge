using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Lodge.Api.FunctionalTests.Users;

public class ExternalLoginTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private const string CallbackUrl = $"{FunctionalTestWebAppFactory.FrontendBaseUrl}/auth/callback";

    [Fact]
    public async Task Providers_Should_BeEmpty_WhenNothingIsConfigured()
    {
        List<ExternalProviderResponse>? providers =
            await HttpClient.GetFromJsonAsync<List<ExternalProviderResponse>>("api/v1/authentication/providers");

        providers.Should().BeEmpty();
    }

    [Fact]
    public async Task Start_Should_RedirectWithAnError_WhenTheProviderIsNotConfigured()
    {
        HttpClient client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("api/v1/authentication/external/google?returnUrl=/trips");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be($"{CallbackUrl}#error=External.ProviderNotConfigured");
    }

    [Fact]
    public async Task Callback_Should_RedirectWithAnError_WhenTheUserCancelled()
    {
        HttpClient client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync(
            "api/v1/authentication/external/github/callback?error=access_denied&state=x");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be($"{CallbackUrl}#error=External.AccessDenied");
    }

    [Fact]
    public async Task ConfiguredProvider_Should_BeListed_AndUseStatePkceAndACorrelationCookie()
    {
        using WebApplicationFactory<Program> configured = Factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Authentication:External:GitHub:ClientId"] = "github-client",
                        ["Authentication:External:GitHub:ClientSecret"] = "github-secret",
                    })));
        // No cookie container: the test decides which cookies are sent.
        HttpClient client = configured.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });

        List<ExternalProviderResponse>? providers =
            await client.GetFromJsonAsync<List<ExternalProviderResponse>>("api/v1/authentication/providers");
        providers.Should().BeEquivalentTo([new ExternalProviderResponse("github", "GitHub")]);

        // An absolute return URL would be an open redirect.
        HttpResponseMessage openRedirect = await client.GetAsync(
            "api/v1/authentication/external/github?returnUrl=//evil.example/");
        openRedirect.Headers.Location!.ToString().Should().Be($"{CallbackUrl}#error=External.InvalidReturnUrl");

        HttpResponseMessage start = await client.GetAsync("api/v1/authentication/external/github?returnUrl=/trips");
        start.StatusCode.Should().Be(HttpStatusCode.Redirect);

        Uri location = start.Headers.Location!;
        location.GetLeftPart(UriPartial.Path).Should().Be("https://github.com/login/oauth/authorize");
        Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query = QueryHelpers.ParseQuery(location.Query);
        query["redirect_uri"].ToString().Should()
            .Be($"{FunctionalTestWebAppFactory.PublicBaseUrl}/api/v1/authentication/external/github/callback");
        query["code_challenge_method"].ToString().Should().Be("S256");
        query["state"].ToString().Should().NotBeNullOrEmpty();

        string cookie = start.Headers.GetValues("Set-Cookie").Single();
        cookie.Should().StartWith("lodge.external-login=")
            .And.Contain("path=/api/v1/authentication/external")
            .And.Contain("samesite=lax")
            .And.Contain("httponly");

        // Without the cookie (e.g. a forged callback in another browser) the state cannot be validated.
        HttpResponseMessage forged = await client.GetAsync(
            $"api/v1/authentication/external/github/callback?code=abc&state={query["state"]}");
        forged.Headers.Location!.ToString().Should().Be($"{CallbackUrl}#error=External.InvalidState");

        // With the cookie but another state.
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/authentication/external/github/callback?code=abc&state=forged");
        request.Headers.Add("Cookie", cookie.Split(';')[0]);
        HttpResponseMessage wrongState = await client.SendAsync(request);
        wrongState.Headers.Location!.ToString().Should().Be($"{CallbackUrl}#error=External.InvalidState");
    }
}
