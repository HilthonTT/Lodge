namespace Lodge.Domain.Wishlists;

/// <summary>
/// Represents the wishlist repository.
/// </summary>
public interface IWishlistRepository
{
    Task<Wishlist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    void Insert(Wishlist wishlist);

    void Remove(Wishlist wishlist);
}
