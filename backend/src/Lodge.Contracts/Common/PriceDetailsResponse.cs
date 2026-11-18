namespace Lodge.Contracts.Common;

/// <summary>
/// Represents the price breakdown of a stay. The total includes the service fee and the taxes.
/// </summary>
public sealed class PriceDetailsResponse
{
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of nights.
    /// </summary>
    public int Nights { get; set; }

    /// <summary>
    /// Gets or sets the sum of the nightly prices, before the discount.
    /// </summary>
    public decimal PricePerPeriod { get; set; }

    /// <summary>
    /// Gets or sets <see cref="PricePerPeriod"/> / <see cref="Nights"/>, rounded to cents.
    /// </summary>
    public decimal AverageNightlyPrice { get; set; }

    /// <summary>
    /// Gets or sets the long-stay discount (0 when none), deducted from <see cref="PricePerPeriod"/>.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets <c>"weekly"</c>, <c>"monthly"</c> or null.
    /// </summary>
    public string? DiscountType { get; set; }

    public decimal CleaningFee { get; set; }

    public decimal AmenitiesUpCharge { get; set; }

    /// <summary>
    /// Gets or sets the guest service fee, on the stay subtotal (nights after discount + up-charge + cleaning fee).
    /// </summary>
    public decimal ServiceFee { get; set; }

    /// <summary>
    /// Gets or sets the occupancy tax, on the nights price after the discount.
    /// </summary>
    public decimal Taxes { get; set; }

    /// <summary>
    /// Gets or sets the occupancy tax rate of the listing country, in percent.
    /// </summary>
    public decimal TaxRatePercent { get; set; }

    public decimal TotalPrice { get; set; }
}
