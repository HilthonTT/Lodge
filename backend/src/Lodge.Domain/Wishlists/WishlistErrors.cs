using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Wishlists;

/// <summary>
/// Contains the wishlist errors.
/// </summary>
public static class WishlistErrors
{
    public static Error NotFound(Guid wishlistId) => Error.NotFound(
        "Wishlist.NotFound",
        $"The wishlist with Id = '{wishlistId}' was not found");

    public static readonly Error NameRequired = Error.Validation(
        "Wishlist.NameRequired",
        "The wishlist name is required");

    public static readonly Error NameTooLong = Error.Validation(
        "Wishlist.NameTooLong",
        $"The wishlist name must not be longer than {Wishlist.NameMaxLength} characters");

    public static readonly Error LimitReached = Error.Conflict(
        "Wishlist.LimitReached",
        $"You can have at most {Wishlist.MaxPerUser} wishlists");
}
