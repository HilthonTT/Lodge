using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Abstractions.Authentication;

/// <summary>
/// Runs the OAuth 2.0 authorization-code flow (with PKCE) against external identity providers.
/// </summary>
public interface IExternalAuthenticationService
{
    /// <summary>
    /// Gets the providers that are configured (have a client id and secret).
    /// </summary>
    IReadOnlyList<ExternalProviderResponse> GetProviders();

    /// <summary>
    /// Creates the redirect to the provider plus the correlation value that must be stored in an HTTP-only cookie
    /// and passed back to <see cref="CompleteAsync"/>.
    /// </summary>
    Result<ExternalLoginChallenge> CreateChallenge(string provider, string returnUrl);

    /// <summary>
    /// Validates the state against the correlation cookie, exchanges the code and fetches the user's identity.
    /// </summary>
    Task<Result<ExternalLoginCallback>> CompleteAsync(
        string provider,
        string? code,
        string? state,
        string? correlation,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// The redirect to an external provider.
/// </summary>
/// <param name="AuthorizationUrl">The provider's authorization URL, including state and PKCE challenge.</param>
/// <param name="Correlation">The protected value to store in the correlation cookie.</param>
public sealed record ExternalLoginChallenge(string AuthorizationUrl, string Correlation);

/// <summary>
/// The verified result of a provider callback.
/// </summary>
public sealed record ExternalLoginCallback(ExternalIdentity Identity, string ReturnUrl);

/// <summary>
/// The user's identity at an external provider.
/// </summary>
/// <param name="Provider">The lower-case provider name.</param>
/// <param name="ProviderKey">The stable user identifier at the provider.</param>
/// <param name="Email">The e-mail address, if the provider shared one.</param>
/// <param name="EmailVerified">Whether the provider has verified <paramref name="Email"/>.</param>
/// <param name="FirstName">The first (given) name, if known.</param>
/// <param name="LastName">The last (family) name, if known.</param>
public sealed record ExternalIdentity(
    string Provider,
    string ProviderKey,
    string? Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName);
