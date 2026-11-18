using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Wishlists;

namespace Lodge.Application.Wishlists;

internal sealed class CreateWishlistCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IWishlistRepository wishlistRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateWishlistCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWishlistCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        if (await wishlistRepository.CountByUserAsync(userId, cancellationToken) >= Wishlist.MaxPerUser)
        {
            return Result.Failure<Guid>(WishlistErrors.LimitReached);
        }

        Result<Wishlist> wishlist = Wishlist.Create(userId, request.Name, dateTimeProvider.UtcNow);

        if (wishlist.IsFailure)
        {
            return Result.Failure<Guid>(wishlist.Error);
        }

        wishlistRepository.Insert(wishlist.Value);

        return wishlist.Value.Id;
    }
}
