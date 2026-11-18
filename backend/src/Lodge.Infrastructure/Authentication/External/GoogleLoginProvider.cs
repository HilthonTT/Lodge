using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Lodge.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.WebUtilities;

namespace Lodge.Infrastructure.Authentication.External;

/// <summary>
/// Google (OpenID Connect) with the authorization-code flow and PKCE.
/// </summary>
internal sealed class GoogleLoginProvider : IExternalLoginProvider
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";

    public string Name => "google";

    public string DisplayName => "Google";

    public ExternalProviderOptions GetOptions(ExternalAuthenticationOptions options) => options.Google;

    public string BuildAuthorizationUrl(ExternalProviderOptions options, string redirectUri, string state, string codeChallenge) =>
        QueryHelpers.AddQueryString(AuthorizationEndpoint, new Dictionary<string, string?>
        {
            ["client_id"] = options.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["prompt"] = "select_account",
        });

    public async Task<ExternalIdentity?> GetIdentityAsync(
        HttpClient httpClient,
        ExternalProviderOptions options,
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = options.ClientId!,
            ["client_secret"] = options.ClientSecret!,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier,
        });

        using HttpResponseMessage tokenResponse = await httpClient.PostAsync(TokenEndpoint, tokenRequest, cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            return null;
        }

        TokenPayload? token = await tokenResponse.Content.ReadFromJsonAsync<TokenPayload>(cancellationToken);

        if (string.IsNullOrEmpty(token?.AccessToken))
        {
            return null;
        }

        using var userRequest = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        using HttpResponseMessage userResponse = await httpClient.SendAsync(userRequest, cancellationToken);

        if (!userResponse.IsSuccessStatusCode)
        {
            return null;
        }

        UserInfoPayload? user = await userResponse.Content.ReadFromJsonAsync<UserInfoPayload>(cancellationToken);

        if (string.IsNullOrEmpty(user?.Subject))
        {
            return null;
        }

        return new ExternalIdentity(Name, user.Subject, user.Email, user.EmailVerified, user.GivenName, user.FamilyName);
    }

    private sealed record TokenPayload([property: JsonPropertyName("access_token")] string? AccessToken);

    private sealed record UserInfoPayload(
        [property: JsonPropertyName("sub")] string? Subject,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("email_verified")] bool EmailVerified,
        [property: JsonPropertyName("given_name")] string? GivenName,
        [property: JsonPropertyName("family_name")] string? FamilyName);
}
