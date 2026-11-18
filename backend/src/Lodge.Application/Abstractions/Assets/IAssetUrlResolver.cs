namespace Lodge.Application.Abstractions.Assets;

/// <summary>
/// Turns stored asset paths into absolute, browser-reachable URLs.
/// </summary>
public interface IAssetUrlResolver
{
    string Resolve(string url);
}
