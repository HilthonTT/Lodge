namespace Lodge.Contracts.Hosting;

/// <summary>
/// Lists or unlists a listing.
/// </summary>
public sealed record UpdateListingStatusRequest(bool IsListed);
