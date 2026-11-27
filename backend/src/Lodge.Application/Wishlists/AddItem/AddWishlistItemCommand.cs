using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Wishlists.AddItem;

/// <summary>
/// Saves a listed apartment in one of the current user's wishlists (idempotent).
/// </summary>
public sealed record AddWishlistItemCommand(Guid WishlistId, Guid ApartmentId) : ICommand;
