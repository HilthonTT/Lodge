using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Apartments;

/// <summary>
/// A host's setting for one night of a listing: blocked and/or a custom nightly price. Only nights with a setting
/// are stored; a day without block and price override is removed.
/// </summary>
public sealed class CalendarDay : Entity
{
    private CalendarDay(Guid id, Guid apartmentId, DateOnly date)
        : base(id)
    {
        ApartmentId = apartmentId;
        Date = date;
    }

    private CalendarDay()
    {
    }

    public Guid ApartmentId { get; private set; }

    /// <summary>
    /// Gets the date of the night (the night from this date to the next day).
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the host has blocked the night.
    /// </summary>
    public bool IsBlocked { get; private set; }

    /// <summary>
    /// Gets the custom nightly price (in the listing's currency), or null.
    /// </summary>
    public decimal? Price { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the day has no setting left (and can be removed).
    /// </summary>
    public bool IsEmpty => !IsBlocked && Price is null;

    public static CalendarDay Create(Guid apartmentId, DateOnly date) => new(Guid.NewGuid(), apartmentId, date);

    public void SetBlocked(bool isBlocked) => IsBlocked = isBlocked;

    public void SetPrice(decimal? price)
    {
        if (price is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), price, "The price must be positive.");
        }

        Price = price;
    }
}
