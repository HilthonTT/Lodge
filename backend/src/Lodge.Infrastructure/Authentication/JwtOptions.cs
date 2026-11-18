using System.ComponentModel.DataAnnotations;

namespace Lodge.Infrastructure.Authentication;

/// <summary>
/// JWT settings (section <c>Jwt</c>). In production supply <see cref="SecurityKey"/> via a secret store
/// or the <c>Jwt__SecurityKey</c> environment variable.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// The HMAC-SHA256 signing key. Must be at least 32 characters (256 bits).
    /// </summary>
    [Required]
    [MinLength(32)]
    public string SecurityKey { get; set; } = string.Empty;

    [Range(1, 7 * 24 * 60)]
    public int TokenExpirationInMinutes { get; set; } = 60;
}
