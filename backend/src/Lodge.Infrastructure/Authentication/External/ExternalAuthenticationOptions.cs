namespace Lodge.Infrastructure.Authentication.External;

/// <summary>
/// Social login providers (section <c>Authentication:External</c>). A provider is enabled when both its client id
/// and client secret are set. Register <c>{App:ApiPublicBaseUrl}/api/v1/authentication/external/{provider}/callback</c>
/// as the redirect/callback URL at the provider.
/// </summary>
public sealed class ExternalAuthenticationOptions
{
    public const string SectionName = "Authentication:External";

    public ExternalProviderOptions Google { get; set; } = new();

    public ExternalProviderOptions GitHub { get; set; } = new();
}
