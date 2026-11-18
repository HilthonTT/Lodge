using System.Net;
using System.Text;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Core.Errors;
using Lodge.Domain.Core.Primitives;
using Lodge.Infrastructure.Authentication.External;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Lodge.Application.UnitTests.Authentication;

public class ExternalAuthenticationServiceTests
{
    private const string ApiBaseUrl = "http://api.test";

    private readonly StubHttpMessageHandler _httpHandler = new();
    private readonly IDataProtectionProvider _dataProtectionProvider = new EphemeralDataProtectionProvider();

    [Fact]
    public void GetProviders_Should_BeEmpty_WhenNothingIsConfigured()
    {
        ExternalAuthenticationService service = CreateService(new ExternalAuthenticationOptions());

        service.GetProviders().Should().BeEmpty();
        service.CreateChallenge("google", "/").Error.Should().Be(ExternalAuthenticationErrors.ProviderNotConfigured);
    }

    [Fact]
    public void GetProviders_Should_ListOnlyConfiguredProviders()
    {
        ExternalAuthenticationService service = CreateService(new ExternalAuthenticationOptions
        {
            GitHub = new ExternalProviderOptions { ClientId = "id", ClientSecret = "secret" },
            Google = new ExternalProviderOptions { ClientId = "id-without-secret" },
        });

        service.GetProviders().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Name = "github", DisplayName = "GitHub" });
    }

    [Fact]
    public void CreateChallenge_Should_RedirectWithStateAndPkce()
    {
        ExternalAuthenticationService service = CreateService(GoogleConfigured());

        ExternalLoginChallenge challenge = service.CreateChallenge("Google", "/trips").Value;

        var uri = new Uri(challenge.AuthorizationUrl);
        Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query = QueryHelpers.ParseQuery(uri.Query);

        uri.GetLeftPart(UriPartial.Path).Should().Be("https://accounts.google.com/o/oauth2/v2/auth");
        query["client_id"].ToString().Should().Be("google-client");
        query["redirect_uri"].ToString().Should().Be($"{ApiBaseUrl}/api/v1/authentication/external/google/callback");
        query["code_challenge_method"].ToString().Should().Be("S256");
        query["code_challenge"].ToString().Should().HaveLength(43);
        query["state"].ToString().Should().HaveLength(43);
        challenge.Correlation.Should().NotContain(query["state"].ToString());
    }

    [Fact]
    public async Task CompleteAsync_Should_RejectAStateThatDoesNotMatchTheCookie()
    {
        ExternalAuthenticationService service = CreateService(GoogleConfigured());
        ExternalLoginChallenge challenge = service.CreateChallenge("google", "/").Value;

        Result<ExternalLoginCallback> wrongState = await service.CompleteAsync("google", "code", "forged", challenge.Correlation);
        Result<ExternalLoginCallback> noCookie = await service.CompleteAsync("google", "code", State(challenge), null);
        Result<ExternalLoginCallback> tamperedCookie =
            await service.CompleteAsync("google", "code", State(challenge), challenge.Correlation + "x");

        wrongState.Error.Should().Be(ExternalAuthenticationErrors.InvalidState);
        noCookie.Error.Should().Be(ExternalAuthenticationErrors.InvalidState);
        tamperedCookie.Error.Should().Be(ExternalAuthenticationErrors.InvalidState);
        _httpHandler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteAsync_Should_RejectACookieOfAnotherProvider()
    {
        ExternalAuthenticationService service = CreateService(new ExternalAuthenticationOptions
        {
            Google = new ExternalProviderOptions { ClientId = "google-client", ClientSecret = "google-secret" },
            GitHub = new ExternalProviderOptions { ClientId = "github-client", ClientSecret = "github-secret" },
        });
        ExternalLoginChallenge challenge = service.CreateChallenge("github", "/").Value;

        Result<ExternalLoginCallback> result =
            await service.CompleteAsync("google", "code", State(challenge), challenge.Correlation);

        result.Error.Should().Be(ExternalAuthenticationErrors.InvalidState);
    }

    [Fact]
    public async Task CompleteAsync_Should_ExchangeTheCodeWithTheVerifier_AndReturnTheGoogleIdentity()
    {
        ExternalAuthenticationService service = CreateService(GoogleConfigured());
        ExternalLoginChallenge challenge = service.CreateChallenge("google", "/apartments/1").Value;

        _httpHandler.Respond("https://oauth2.googleapis.com/token", """{"access_token":"google-access"}""");
        _httpHandler.Respond(
            "https://openidconnect.googleapis.com/v1/userinfo",
            """{"sub":"1234","email":"jane@gmail.com","email_verified":true,"given_name":"Jane","family_name":"Doe"}""");

        Result<ExternalLoginCallback> result =
            await service.CompleteAsync("google", "auth-code", State(challenge), challenge.Correlation);

        result.Value.Should().Be(new ExternalLoginCallback(
            new ExternalIdentity("google", "1234", "jane@gmail.com", true, "Jane", "Doe"),
            "/apartments/1"));

        string tokenRequestBody = _httpHandler.Requests[0].Body;
        tokenRequestBody.Should().Contain("code=auth-code").And.Contain("code_verifier=").And.Contain("grant_type=authorization_code");
        _httpHandler.Requests[1].Authorization.Should().Be("Bearer google-access");
    }

    [Fact]
    public async Task CompleteAsync_Should_UseTheVerifiedPrimaryGitHubEmail()
    {
        ExternalAuthenticationService service = CreateService(new ExternalAuthenticationOptions
        {
            GitHub = new ExternalProviderOptions { ClientId = "github-client", ClientSecret = "github-secret" },
        });
        ExternalLoginChallenge challenge = service.CreateChallenge("github", "/").Value;

        _httpHandler.Respond("https://github.com/login/oauth/access_token", """{"access_token":"gh-access"}""");
        _httpHandler.Respond("https://api.github.com/user", """{"id":42,"login":"octocat","name":"Mona Lisa Octocat"}""");
        _httpHandler.Respond(
            "https://api.github.com/user/emails",
            """[{"email":"old@example.com","primary":false,"verified":false},{"email":"mona@github.test","primary":true,"verified":true}]""");

        Result<ExternalLoginCallback> result =
            await service.CompleteAsync("github", "auth-code", State(challenge), challenge.Correlation);

        result.Value.Identity.Should().Be(
            new ExternalIdentity("github", "42", "mona@github.test", true, "Mona", "Lisa Octocat"));
    }

    [Fact]
    public async Task CompleteAsync_Should_FailCleanly_WhenTheProviderRejectsTheCode()
    {
        ExternalAuthenticationService service = CreateService(GoogleConfigured());
        ExternalLoginChallenge challenge = service.CreateChallenge("google", "/").Value;

        _httpHandler.Respond("https://oauth2.googleapis.com/token", """{"error":"invalid_grant"}""", HttpStatusCode.BadRequest);

        Result<ExternalLoginCallback> result =
            await service.CompleteAsync("google", "bad-code", State(challenge), challenge.Correlation);

        result.Error.Should().Be(ExternalAuthenticationErrors.ProviderError);
    }

    private static string State(ExternalLoginChallenge challenge) =>
        QueryHelpers.ParseQuery(new Uri(challenge.AuthorizationUrl).Query)["state"].ToString();

    private static ExternalAuthenticationOptions GoogleConfigured() => new()
    {
        Google = new ExternalProviderOptions { ClientId = "google-client", ClientSecret = "google-secret" },
    };

    private ExternalAuthenticationService CreateService(ExternalAuthenticationOptions options)
    {
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(_httpHandler, disposeHandler: false));

        return new ExternalAuthenticationService(
            [new GoogleLoginProvider(), new GitHubLoginProvider()],
            Options.Create(options),
            Options.Create(new AppOptions { ApiPublicBaseUrl = ApiBaseUrl, FrontendBaseUrl = "http://frontend.test" }),
            _dataProtectionProvider,
            httpClientFactory,
            NullLogger<ExternalAuthenticationService>.Instance);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (string Body, HttpStatusCode Status)> _responses = [];

        public List<(string Url, string Body, string? Authorization)> Requests { get; } = [];

        public void Respond(string url, string json, HttpStatusCode status = HttpStatusCode.OK) =>
            _responses[url] = (json, status);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string url = request.RequestUri!.GetLeftPart(UriPartial.Path);
            string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((url, body, request.Headers.Authorization?.ToString()));

            if (!_responses.TryGetValue(url, out (string Body, HttpStatusCode Status) response))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return new HttpResponseMessage(response.Status)
            {
                Content = new StringContent(response.Body, Encoding.UTF8, "application/json"),
            };
        }
    }
}
