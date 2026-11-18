using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Wishlists;

namespace Lodge.Application.Wishlists;

internal sealed class RenameWishlistCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IWishlistRepository wishlistRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RenameWishlistCommand>
{
    public async Task<Result> Handle(RenameWishlistCommand request, CancellationToken cancellationToken)
    {
        Result<Wishlist> wishlist = await wishlistRepository.GetOwnAsync(
            request.WishlistId,
            userIdentifierProvider.UserId,
            cancellationToken);

        return wishlist.IsFailure ? wishlist : wishlist.Value.Rename(request.Name, dateTimeProvider.UtcNow);
    }
}
