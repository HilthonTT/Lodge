using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Wishlists;

namespace Lodge.Application.Wishlists;

internal sealed class RemoveWishlistItemCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IWishlistRepository wishlistRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RemoveWishlistItemCommand>
{
    public async Task<Result> Handle(RemoveWishlistItemCommand request, CancellationToken cancellationToken)
    {
        Result<Wishlist> wishlist = await wishlistRepository.GetOwnAsync(
            request.WishlistId,
            userIdentifierProvider.UserId,
            cancellationToken);

        if (wishlist.IsFailure)
        {
            return wishlist;
        }

        wishlist.Value.RemoveItem(request.ApartmentId, dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
