using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Wishlists;

namespace Lodge.Application.Wishlists;

/// <summary>
/// Creates a wishlist for the current user (at most <see cref="Wishlist.MaxPerUser"/>).
/// </summary>
public sealed record CreateWishlistCommand(string? Name) : ICommand<Guid>;
