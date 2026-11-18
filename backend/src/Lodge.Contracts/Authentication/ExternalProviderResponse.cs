namespace Lodge.Contracts.Authentication;

/// <summary>
/// A configured external (social) login provider.
/// </summary>
/// <param name="Name">The provider name used in routes: <c>google</c> or <c>github</c>.</param>
/// <param name="DisplayName">The name to show on the button, e.g. <c>GitHub</c>.</param>
public sealed record ExternalProviderResponse(string Name, string DisplayName);
