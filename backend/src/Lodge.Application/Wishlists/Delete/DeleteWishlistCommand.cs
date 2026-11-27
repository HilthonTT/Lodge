using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Wishlists.Delete;

/// <summary>
/// Deletes one of the current user's wishlists.
/// </summary>
public sealed record DeleteWishlistCommand(Guid WishlistId) : ICommand;
