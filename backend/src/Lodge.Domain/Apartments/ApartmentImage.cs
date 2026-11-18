namespace Lodge.Domain.Apartments;

/// <summary>
/// A photo of a listing. Either a bundled asset (<see cref="Path"/>, e.g. <c>/images/cabin.png</c>) or an uploaded
/// file, whose blob identifier is <see cref="Id"/>.
/// </summary>
public sealed class ApartmentImage
{
    private ApartmentImage(Guid id, string? path)
    {
        Id = id;
        Path = path;
    }

    private ApartmentImage()
    {
    }

    /// <summary>
    /// Gets the image identifier. For uploaded photos this is the blob (file) identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the relative path of a bundled asset, or null for an uploaded file.
    /// </summary>
    public string? Path { get; private set; }

    /// <summary>
    /// Gets the zero-based display position; position 0 is the cover photo.
    /// </summary>
    public int Position { get; internal set; }

    public bool IsUploadedFile => Path is null;

    public static ApartmentImage FromFile(Guid fileId)
    {
        if (fileId == Guid.Empty)
        {
            throw new ArgumentException("The file identifier is required.", nameof(fileId));
        }

        return new ApartmentImage(fileId, null);
    }

    public static ApartmentImage FromPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return new ApartmentImage(Guid.NewGuid(), path);
    }
}
