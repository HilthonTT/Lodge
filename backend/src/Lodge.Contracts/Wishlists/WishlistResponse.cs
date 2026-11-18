using Lodge.Contracts.Apartments;

namespace Lodge.Contracts.Wishlists;

/// <summary>
/// A wishlist with its (still listed) apartments, most recently saved first.
/// </summary>
public sealed record WishlistResponse(Guid Id, string Name, IReadOnlyList<ApartmentResponse> Items);
