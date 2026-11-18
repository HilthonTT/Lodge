using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Authentication.External;

/// <summary>
/// Handles the provider callback: validates it, resolves (or creates) the user and signs them in.
/// </summary>
/// <param name="Provider">The provider name from the route.</param>
/// <param name="Code">The authorization code.</param>
/// <param name="State">The state parameter.</param>
/// <param name="ProviderError">The <c>error</c> parameter sent by the provider, if any.</param>
/// <param name="Correlation">The value of the correlation cookie.</param>
public sealed record CompleteExternalLoginCommand(
    string Provider,
    string? Code,
    string? State,
    string? ProviderError,
    string? Correlation) : ICommand<ExternalLoginResult>;
