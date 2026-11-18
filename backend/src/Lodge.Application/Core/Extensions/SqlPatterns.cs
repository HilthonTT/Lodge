namespace Lodge.Application.Core.Extensions;

/// <summary>
/// Builds SQL <c>LIKE</c> patterns from user input.
/// </summary>
internal static class SqlPatterns
{
    /// <summary>
    /// Returns <c>%text%</c> with the <c>LIKE</c> wildcards (and the escape character) of the text escaped.
    /// </summary>
    public static string Contains(string text)
    {
        string escaped = text.Trim()
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);

        return $"%{escaped}%";
    }
}
