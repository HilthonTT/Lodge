namespace Lodge.Contracts.Hosting;

/// <summary>
/// An uploaded listing photo; pass <paramref name="FileId"/> in <see cref="ListingRequest.ImageIds"/>.
/// </summary>
public sealed record PhotoUploadResponse(Guid FileId, string Url);
