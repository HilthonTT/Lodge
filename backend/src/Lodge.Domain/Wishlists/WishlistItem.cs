namespace Lodge.Domain.Wishlists;

/// <summary>
/// A listing saved in a wishlist.
/// </summary>
public sealed class WishlistItem
{
    internal WishlistItem(Guid apartmentId, DateTime addedOnUtc)
    {
        ApartmentId = apartmentId;
        AddedOnUtc = addedOnUtc;
    }

    private WishlistItem()
    {
    }

    public Guid ApartmentId { get; private set; }

    public DateTime AddedOnUtc { get; private set; }
}
