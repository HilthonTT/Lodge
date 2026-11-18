using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Wishlists;

namespace Lodge.Application.Wishlists;

/// <summary>
/// Gets one of the current user's wishlists with its listed apartments, most recently saved first.
/// </summary>
public sealed record GetWishlistByIdQuery(Guid WishlistId) : IQuery<WishlistResponse>;
