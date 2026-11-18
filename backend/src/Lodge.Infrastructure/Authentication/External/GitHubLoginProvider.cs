using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Lodge.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.WebUtilities;

namespace Lodge.Infrastructure.Authentication.External;

/// <summary>
/// GitHub OAuth app with the authorization-code flow and PKCE. The e-mail address comes from
/// <c>/user/emails</c>, which tells whether it is verified.
/// </summary>
internal sealed class GitHubLoginProvider : IExternalLoginProvider
{
    private const string AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
    private const string TokenEndpoint = "https://github.com/login/oauth/access_token";
    private const string UserEndpoint = "https://api.github.com/user";
    private const string EmailsEndpoint = "https://api.github.com/user/emails";

    public string Name => "github";

    public string DisplayName => "GitHub";

    public ExternalProviderOptions GetOptions(ExternalAuthenticationOptions options) => options.GitHub;

    public string BuildAuthorizationUrl(ExternalProviderOptions options, string redirectUri, string state, string codeChallenge) =>
        QueryHelpers.AddQueryString(AuthorizationEndpoint, new Dictionary<string, string?>
        {
            ["client_id"] = options.ClientId,
            ["redirect_uri"] = redirectUri,
            ["scope"] = "read:user user:email",
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["allow_signup"] = "true",
        });

    public async Task<ExternalIdentity?> GetIdentityAsync(
        HttpClient httpClient,
        ExternalProviderOptions options,
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = options.ClientId!,
                ["client_secret"] = options.ClientSecret!,
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["code_verifier"] = codeVerifier,
            }),
        };
        tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using HttpResponseMessage tokenResponse = await httpClient.SendAsync(tokenRequest, cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            return null;
        }

        // GitHub reports errors (e.g. bad_verification_code) with 200 OK and an "error" field.
        TokenPayload? token = await tokenResponse.Content.ReadFromJsonAsync<TokenPayload>(cancellationToken);

        if (string.IsNullOrEmpty(token?.AccessToken))
        {
            return null;
        }

        UserPayload? user = await GetAsync<UserPayload>(httpClient, UserEndpoint, token.AccessToken, cancellationToken);

        if (user is null || user.Id == 0)
        {
            return null;
        }

        List<EmailPayload> emails =
            await GetAsync<List<EmailPayload>>(httpClient, EmailsEndpoint, token.AccessToken, cancellationToken) ?? [];

        EmailPayload? email = emails.Find(e => e is { Primary: true, Verified: true }) ?? emails.Find(e => e.Verified);

        (string? firstName, string? lastName) = SplitName(user.Name ?? user.Login);

        return new ExternalIdentity(
            Name,
            user.Id.ToString(CultureInfo.InvariantCulture),
            email?.Email,
            email is not null,
            firstName,
            lastName);
    }

    internal static (string? FirstName, string? LastName) SplitName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (null, null);
        }

        string[] parts = name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length == 1 ? (parts[0], null) : (parts[0], parts[1]);
    }

    private static async Task<T?> GetAsync<T>(HttpClient httpClient, string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            : default;
    }

    private sealed record TokenPayload([property: JsonPropertyName("access_token")] string? AccessToken);

    private sealed record UserPayload(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("login")] string? Login,
        [property: JsonPropertyName("name")] string? Name);

    private sealed record EmailPayload(
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("primary")] bool Primary,
        [property: JsonPropertyName("verified")] bool Verified);
}
