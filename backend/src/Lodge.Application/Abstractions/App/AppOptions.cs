using System.ComponentModel.DataAnnotations;

namespace Lodge.Application.Abstractions.App;

/// <summary>
/// Public URLs of the application (section <c>App</c>).
/// </summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// The browser-facing URL of the frontend. Links in e-mails (<c>/verify-email</c>, <c>/reset-password</c>) and the
    /// social login result (<c>/auth/callback</c>) point here.
    /// </summary>
    [Required]
    [Url]
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";

    /// <summary>
    /// The browser-facing URL of the API, i.e. the gateway. Used for OAuth redirect URIs
    /// (<c>{ApiPublicBaseUrl}/api/v1/authentication/external/{provider}/callback</c>) and avatar URLs.
    /// </summary>
    [Required]
    [Url]
    public string ApiPublicBaseUrl { get; set; } = "http://localhost:8000";

    public string FrontendUrl(string pathAndQuery) => $"{FrontendBaseUrl.TrimEnd('/')}/{pathAndQuery.TrimStart('/')}";

    public string ApiUrl(string pathAndQuery) => $"{ApiPublicBaseUrl.TrimEnd('/')}/{pathAndQuery.TrimStart('/')}";

    /// <summary>
    /// The public URL of an uploaded file (e.g. an avatar).
    /// </summary>
    public string FileUrl(Guid fileId) => ApiUrl($"api/v1/files/{fileId}");
}
