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
