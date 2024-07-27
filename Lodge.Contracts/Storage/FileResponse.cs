namespace Lodge.Contracts.Storage;

/// <summary>
/// Represents the file response.
/// </summary>
/// <param name="Stream">The file stream.</param>
/// <param name="ContentType">The file content type.</param>
public sealed record FileResponse(Stream Stream, string ContentType);
