namespace Lodge.Contracts.Common;

/// <summary>
/// Represents the price details response
/// </summary>
public sealed class PriceDetailsResponse
{
    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the price per period.
    /// </summary>
    public decimal PricePerPeriod { get; set; }

    /// <summary>
    /// Gets or sets the cleaning fee.
    /// </summary>
    public decimal CleaningFee { get; set; }

    /// <summary>
    /// Gets or sets the amenities up charge.
    /// </summary>
    public decimal AmenitiesUpCharge { get; set; }

    /// <summary>
    /// Gets or sets the total price.
    /// </summary>
    public decimal TotalPrice { get; set; }
}
