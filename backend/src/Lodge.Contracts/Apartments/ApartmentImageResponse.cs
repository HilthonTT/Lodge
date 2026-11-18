namespace Lodge.Contracts.Apartments;

/// <summary>
/// A photo of a listing.
/// </summary>
/// <param name="Id">The photo identifier (for uploaded photos, the file identifier).</param>
/// <param name="Url">The absolute URL.</param>
public sealed record ApartmentImageResponse(Guid Id, string Url);
