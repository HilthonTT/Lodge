namespace Lodge.Contracts.Users;

/// <summary>
/// A host's published review of a guest.
/// </summary>
public sealed record GuestReviewResponse(
    Guid Id,
    int Rating,
    string Comment,
    string HostFirstName,
    string? HostAvatarUrl,
    string ListingName,
    DateTime CreatedOnUtc);
