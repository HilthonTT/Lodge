using FluentValidation;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Wishlists;

namespace Lodge.Application.Wishlists;

internal static class WishlistValidation
{
    public static IRuleBuilderOptions<T, string?> ValidWishlistName<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithError(WishlistErrors.NameRequired)
            .Must(name => name is null || name.Trim().Length <= Wishlist.NameMaxLength).WithError(WishlistErrors.NameTooLong);

    /// <summary>
    /// Loads a wishlist of the current user; other users' wishlists are "not found".
    /// </summary>
    public static async Task<Result<Wishlist>> GetOwnAsync(
        this IWishlistRepository repository,
        Guid wishlistId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        Wishlist? wishlist = await repository.GetByIdAsync(wishlistId, cancellationToken);

        return wishlist is not null && wishlist.UserId == userId
            ? wishlist
            : Result.Failure<Wishlist>(WishlistErrors.NotFound(wishlistId));
    }
}
