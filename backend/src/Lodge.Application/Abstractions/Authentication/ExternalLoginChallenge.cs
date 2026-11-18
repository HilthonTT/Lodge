namespace Lodge.Application.Abstractions.Authentication;

/// <summary>
/// The redirect to an external provider.
/// </summary>
/// <param name="AuthorizationUrl">The provider's authorization URL, including state and PKCE challenge.</param>
/// <param name="Correlation">The protected value to store in the correlation cookie.</param>
public sealed record ExternalLoginChallenge(string AuthorizationUrl, string Correlation);
