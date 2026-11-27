using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Wishlists.RemoveItem;

/// <summary>
/// Removes an apartment from one of the current user's wishlists (idempotent).
/// </summary>
public sealed record RemoveWishlistItemCommand(Guid WishlistId, Guid ApartmentId) : ICommand;
