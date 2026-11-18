namespace Lodge.Contracts.Apartments;

/// <summary>
/// The host of a listing.
/// </summary>
public sealed record HostSummaryResponse(
    Guid Id,
    string FirstName,
    string? AvatarUrl,
    bool IsSuperhost,
    DateTime JoinedOnUtc);
