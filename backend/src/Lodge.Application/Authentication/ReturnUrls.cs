namespace Lodge.Application.Authentication;

/// <summary>
/// Validates return URLs so that the login flow cannot be used as an open redirect.
/// </summary>
public static class ReturnUrls
{
    public const string Default = "/";

    private const int MaxLength = 2048;

    /// <summary>
    /// Returns true for a same-site relative path such as <c>/apartments/1?x=y</c>. Absolute URLs,
    /// protocol-relative URLs (<c>//evil.com</c>), backslashes and control characters are rejected.
    /// </summary>
    public static bool IsLocal(string? url)
    {
        if (string.IsNullOrEmpty(url) || url.Length > MaxLength || url[0] != '/')
        {
            return false;
        }

        if (url.Length > 1 && (url[1] == '/' || url[1] == '\\'))
        {
            return false;
        }

        return !url.Any(c => char.IsControl(c) || c == '\\');
    }
}
