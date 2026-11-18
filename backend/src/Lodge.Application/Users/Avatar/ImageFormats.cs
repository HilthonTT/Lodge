namespace Lodge.Application.Users.Avatar;

/// <summary>
/// Detects JPEG, PNG and WebP images from their leading bytes ("magic numbers").
/// </summary>
internal static class ImageFormats
{
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string Webp = "image/webp";

    public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Jpeg, Png, Webp,
    };

    private static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static ReadOnlySpan<byte> Riff => "RIFF"u8;

    private static ReadOnlySpan<byte> WebpFourCc => "WEBP"u8;

    /// <summary>
    /// Returns the content type of the image, or null when it is not a JPEG, PNG or WebP image.
    /// </summary>
    public static string? Detect(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(JpegSignature))
        {
            return Jpeg;
        }

        if (header.StartsWith(PngSignature))
        {
            return Png;
        }

        if (header.Length >= 12 && header.StartsWith(Riff) && header.Slice(8, 4).SequenceEqual(WebpFourCc))
        {
            return Webp;
        }

        return null;
    }
}
