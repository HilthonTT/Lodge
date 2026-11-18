using Lodge.Contracts.Apartments;

namespace Lodge.Contracts.Wishlists;

/// <summary>
/// Creates or renames a wishlist.
/// </summary>
public sealed record WishlistNameRequest(string Name);

/// <summary>
/// A wishlist in the list of the user's wishlists.
/// </summary>
/// <param name="Id">The wishlist identifier.</param>
/// <param name="Name">The name.</param>
/// <param name="ItemCount">The number of (still listed) saved apartments.</param>
/// <param name="CoverImageUrl">The cover photo of the most recently saved apartment, or null when empty.</param>
/// <param name="UpdatedOnUtc">When the name or the items last changed.</param>
public sealed record WishlistSummaryResponse(
    Guid Id,
    string Name,
    int ItemCount,
    string? CoverImageUrl,
    DateTime UpdatedOnUtc);

/// <summary>
/// A wishlist with its (still listed) apartments, most recently saved first.
/// </summary>
public sealed record WishlistResponse(Guid Id, string Name, IReadOnlyList<ApartmentResponse> Items);
