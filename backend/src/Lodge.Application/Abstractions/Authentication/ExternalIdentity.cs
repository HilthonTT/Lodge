namespace Lodge.Application.Abstractions.Authentication;

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
