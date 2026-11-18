namespace Lodge.Infrastructure.Authentication.Settings;

/// <summary>
/// Represents the JWT configuration settings.
/// </summary>
internal sealed class JwtSettings
{
    public const string SettingsKey = "Jwt";

    /// <summary>
    /// Gets or sets the issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the security key.
    /// </summary>
    public string SecurityKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiration time in minutes.
    /// </summary>
    public int TokenExpirationInMinutes { get; set; }
}
