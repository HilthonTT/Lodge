using Microsoft.Extensions.Options;

namespace Lodge.Application.Abstractions.Assets;

/// <summary>
/// Prefixes relative asset paths with <see cref="AssetsOptions.PublicBaseUrl"/>. Absolute URLs are returned unchanged.
/// </summary>
internal sealed class AssetUrlResolver(IOptions<AssetsOptions> options) : IAssetUrlResolver
{
    private readonly string? _baseUrl = options.Value.PublicBaseUrl?.TrimEnd('/');

    public string Resolve(string url)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            string.IsNullOrWhiteSpace(_baseUrl) ||
            Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return url;
        }

        return $"{_baseUrl}/{url.TrimStart('/')}";
    }
}
