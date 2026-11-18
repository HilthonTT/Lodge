using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Users.Avatar;

/// <summary>
/// Detects JPEG, PNG and WebP images from their leading bytes ("magic numbers"). Shared by every image upload
/// (avatars, listing and experience photos, <c>POST /files</c>).
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

    /// <summary>
    /// Buffers an uploaded image and checks its size and leading bytes; the declared content type is not trusted.
    /// On success the returned buffer is positioned at 0 and the content type is the detected one.
    /// </summary>
    /// <param name="source">The uploaded stream.</param>
    /// <param name="maxBytes">The maximum size in bytes.</param>
    /// <param name="invalidImage">The error when the file is empty or not a JPEG, PNG or WebP image.</param>
    /// <param name="tooLarge">The error when the file is larger than <paramref name="maxBytes"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task<Result<BufferedImage>> BufferAsync(
        Stream source,
        long maxBytes,
        Error invalidImage,
        Error tooLarge,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        var buffer = new MemoryStream();
        byte[] chunk = new byte[81920];
        int read;

        // Reads at most one byte more than allowed, so an oversized upload is never fully buffered.
        while ((read = await source.ReadAsync(
                   chunk.AsMemory(0, (int)Math.Min(chunk.Length, maxBytes + 1 - buffer.Length)),
                   cancellationToken)) > 0)
        {
            buffer.Write(chunk, 0, read);

            if (buffer.Length > maxBytes)
            {
                await buffer.DisposeAsync();
                return Result.Failure<BufferedImage>(tooLarge);
            }
        }

        string? contentType = Detect(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, 16)));

        if (contentType is null)
        {
            await buffer.DisposeAsync();
            return Result.Failure<BufferedImage>(invalidImage);
        }

        buffer.Position = 0;

        return new BufferedImage(buffer, contentType);
    }
}

/// <summary>
/// A validated, in-memory image upload.
/// </summary>
/// <param name="Content">The image bytes, positioned at 0.</param>
/// <param name="ContentType">The detected content type.</param>
internal sealed record BufferedImage(MemoryStream Content, string ContentType) : IDisposable
{
    public void Dispose() => Content.Dispose();
}
