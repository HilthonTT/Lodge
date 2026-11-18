using Lodge.Domain.Wishlists;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the wishlist repository.
/// </summary>
/// <param name="context">The database context.</param>
internal sealed class WishlistRepository(LodgeDbContext context) : IWishlistRepository
{
    /// <inheritdoc />
    public Task<Wishlist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Wishlists.FirstOrDefaultAsync(wishlist => wishlist.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Wishlists.CountAsync(wishlist => wishlist.UserId == userId, cancellationToken);

    /// <inheritdoc />
    public void Insert(Wishlist wishlist) => context.Wishlists.Add(wishlist);

    /// <inheritdoc />
    public void Remove(Wishlist wishlist) => context.Wishlists.Remove(wishlist);
}
