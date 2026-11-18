using Lodge.Domain.Shared;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Represents the price breakdown of a stay.
/// </summary>
/// <param name="Nights">The number of nights.</param>
/// <param name="PriceForPeriod">The sum of the nightly prices, before the discount.</param>
/// <param name="Discount">The weekly or monthly discount on <paramref name="PriceForPeriod"/> (zero when none).</param>
/// <param name="DiscountType">Which discount was applied.</param>
/// <param name="CleaningFee">The cleaning fee.</param>
/// <param name="AmenitiesUpCharge">The amenity up-charge, on the discounted nights price.</param>
/// <param name="ServiceFee">The guest service fee, on the stay subtotal (<see cref="StaySubtotal"/>).</param>
/// <param name="Taxes">The occupancy tax, on the nights price after the discount.</param>
/// <param name="TaxRatePercent">The occupancy tax rate, in percent.</param>
/// <param name="TotalPrice">The stay subtotal plus the service fee and the taxes.</param>
public sealed record PricingDetails(
    int Nights,
    Money PriceForPeriod,
    Money Discount,
    StayDiscountType DiscountType,
    Money CleaningFee,
    Money AmenitiesUpCharge,
    Money ServiceFee,
    Money Taxes,
    decimal TaxRatePercent,
    Money TotalPrice)
{
    /// <summary>
    /// Gets the nights price after the discount, plus the amenity up-charge and the cleaning fee: what the host earns
    /// before the host service fee.
    /// </summary>
    public Money StaySubtotal => new(
        PriceForPeriod.Amount - Discount.Amount + AmenitiesUpCharge.Amount + CleaningFee.Amount,
        PriceForPeriod.Currency);

    /// <summary>
    /// Gets the average nightly price before the discount, rounded to cents.
    /// </summary>
    public Money AverageNightlyPrice => new(
        Nights == 0 ? 0 : Math.Round(PriceForPeriod.Amount / Nights, 2, MidpointRounding.AwayFromZero),
        PriceForPeriod.Currency);
}
