using Lodge.Application.Abstractions.Authentication;

namespace Lodge.Infrastructure.Authentication.External;

/// <summary>
/// An OAuth 2.0 provider: builds the authorization URL and turns an authorization code into an identity.
/// </summary>
internal interface IExternalLoginProvider
{
    /// <summary>
    /// The lower-case route name, e.g. <c>google</c>.
    /// </summary>
    string Name { get; }

    string DisplayName { get; }

    ExternalProviderOptions GetOptions(ExternalAuthenticationOptions options);

    string BuildAuthorizationUrl(ExternalProviderOptions options, string redirectUri, string state, string codeChallenge);

    /// <summary>
    /// Exchanges the code and fetches the user. Returns null when the provider rejects the code or fails.
    /// </summary>
    Task<ExternalIdentity?> GetIdentityAsync(
        HttpClient httpClient,
        ExternalProviderOptions options,
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken);
}
