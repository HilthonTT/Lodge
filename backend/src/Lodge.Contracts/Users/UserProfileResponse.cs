using Lodge.Contracts.Apartments;

namespace Lodge.Contracts.Users;

/// <summary>
/// The public profile of a user. It deliberately omits the e-mail address and the full last name.
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="LastInitial">The first letter of the last name, upper case (e.g. <c>L</c>).</param>
/// <param name="AvatarUrl">The absolute avatar URL, or null.</param>
/// <param name="Bio">The biography, or null.</param>
/// <param name="JoinedOnUtc">When the account was created.</param>
/// <param name="EmailVerified">Whether the e-mail address is verified.</param>
/// <param name="ReviewsWrittenCount">The number of reviews the user has written.</param>
/// <param name="IsSuperhost">Whether the user is a Superhost.</param>
/// <param name="HostingSince">When the user created their first (not deleted) listing, or null.</param>
/// <param name="Listings">The user's listed apartments, newest first (at most 12).</param>
/// <param name="HostReviewCount">The number of reviews of the user's listings.</param>
/// <param name="HostAverageRating">The average rating of the user's listings (1 decimal), or null.</param>
/// <param name="GuestReviewCount">The number of published reviews hosts wrote about the user as a guest.</param>
/// <param name="GuestAverageRating">The average of those ratings (1 decimal), or null.</param>
/// <param name="GuestReviews">The latest 10 of those reviews, newest first.</param>
public sealed record UserProfileResponse(
    Guid Id,
    string FirstName,
    string LastInitial,
    string? AvatarUrl,
    string? Bio,
    DateTime JoinedOnUtc,
    bool EmailVerified,
    int ReviewsWrittenCount,
    bool IsSuperhost,
    DateTime? HostingSince,
    IReadOnlyList<ApartmentResponse> Listings,
    int HostReviewCount,
    decimal? HostAverageRating,
    int GuestReviewCount,
    decimal? GuestAverageRating,
    IReadOnlyList<GuestReviewResponse> GuestReviews);

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
