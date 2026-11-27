using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Wishlists.Rename;

/// <summary>
/// Renames one of the current user's wishlists.
/// </summary>
public sealed record RenameWishlistCommand(Guid WishlistId, string? Name) : ICommand;
