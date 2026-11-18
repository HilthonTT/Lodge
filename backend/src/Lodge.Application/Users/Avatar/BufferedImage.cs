namespace Lodge.Application.Users.Avatar;

/// <summary>
/// A validated, in-memory image upload.
/// </summary>
/// <param name="Content">The image bytes, positioned at 0.</param>
/// <param name="ContentType">The detected content type.</param>
internal sealed record BufferedImage(MemoryStream Content, string ContentType) : IDisposable
{
    public void Dispose() => Content.Dispose();
}
