namespace Lodge.Infrastructure.Authentication.External;

/// <summary>
/// The OAuth client credentials of a provider.
/// </summary>
public sealed class ExternalProviderOptions
{
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
