using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Wishlists;

namespace Lodge.Application.Wishlists.AddItem;

internal sealed class AddWishlistItemCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IWishlistRepository wishlistRepository,
    IApartmentRepository apartmentRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AddWishlistItemCommand>
{
    public async Task<Result> Handle(AddWishlistItemCommand request, CancellationToken cancellationToken)
    {
        Result<Wishlist> wishlist = await wishlistRepository.GetOwnAsync(
            request.WishlistId,
            userIdentifierProvider.UserId,
            cancellationToken);

        if (wishlist.IsFailure)
        {
            return wishlist;
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is not { IsListed: true })
        {
            return Result.Failure(ApartmentErrors.NotFound(request.ApartmentId));
        }

        wishlist.Value.AddItem(apartment.Id, dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
