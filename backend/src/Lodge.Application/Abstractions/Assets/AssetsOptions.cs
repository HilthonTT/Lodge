namespace Lodge.Application.Abstractions.Assets;

/// <summary>
/// Settings for assets (e.g. apartment images) served by the API itself.
/// </summary>
public sealed class AssetsOptions
{
    public const string SectionName = "Assets";

    /// <summary>
    /// The public, browser-reachable base URL that relative asset paths (e.g. <c>/images/cabin.png</c>)
    /// are resolved against, e.g. <c>http://localhost:8000</c>. When empty, relative paths are returned as-is.
    /// </summary>
    public string? PublicBaseUrl { get; set; }
}
