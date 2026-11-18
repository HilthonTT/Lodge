using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Wishlists;

namespace Lodge.Application.Wishlists;

internal sealed class DeleteWishlistCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IWishlistRepository wishlistRepository) : ICommandHandler<DeleteWishlistCommand>
{
    public async Task<Result> Handle(DeleteWishlistCommand request, CancellationToken cancellationToken)
    {
        Result<Wishlist> wishlist = await wishlistRepository.GetOwnAsync(
            request.WishlistId,
            userIdentifierProvider.UserId,
            cancellationToken);

        if (wishlist.IsFailure)
        {
            return wishlist;
        }

        wishlistRepository.Remove(wishlist.Value);

        return Result.Success();
    }
}
