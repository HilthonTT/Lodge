using Lodge.Domain.Shared;

namespace Lodge.Domain.Bookings;

/// <summary>
/// The discount applied to long stays.
/// </summary>
public enum StayDiscountType
{
    None = 0,

    /// <summary>
    /// The weekly discount (stays of at least 7 nights).
    /// </summary>
    Weekly = 1,

    /// <summary>
    /// The monthly discount (stays of at least 28 nights).
    /// </summary>
    Monthly = 2,
}

/// <summary>
/// Represents the price breakdown of a stay.
/// </summary>
/// <param name="Nights">The number of nights.</param>
/// <param name="PriceForPeriod">The sum of the nightly prices, before the discount.</param>
/// <param name="Discount">The weekly or monthly discount on <paramref name="PriceForPeriod"/> (zero when none).</param>
/// <param name="DiscountType">Which discount was applied.</param>
/// <param name="CleaningFee">The cleaning fee.</param>
/// <param name="AmenitiesUpCharge">The amenity up-charge, on the discounted nights price.</param>
/// <param name="TotalPrice">The nights price minus the discount, plus the up-charge and the cleaning fee.</param>
public sealed record PricingDetails(
    int Nights,
    Money PriceForPeriod,
    Money Discount,
    StayDiscountType DiscountType,
    Money CleaningFee,
    Money AmenitiesUpCharge,
    Money TotalPrice)
{
    /// <summary>
    /// Gets the average nightly price before the discount, rounded to cents.
    /// </summary>
    public Money AverageNightlyPrice => new(
        Nights == 0 ? 0 : Math.Round(PriceForPeriod.Amount / Nights, 2, MidpointRounding.AwayFromZero),
        PriceForPeriod.Currency);
}
