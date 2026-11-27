using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Wishlists;

namespace Lodge.Application.Wishlists.Get;

/// <summary>
/// Gets the current user's wishlists, most recently changed first. Unlisted and deleted apartments are not counted.
/// </summary>
public sealed record GetWishlistsQuery : IQuery<List<WishlistSummaryResponse>>;
