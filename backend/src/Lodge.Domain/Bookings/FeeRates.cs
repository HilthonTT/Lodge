namespace Lodge.Domain.Bookings;

/// <summary>
/// The guest service fee and the occupancy tax applied to a stay.
/// </summary>
/// <param name="GuestServiceFeePercent">The guest service fee, in percent of the stay subtotal.</param>
/// <param name="TaxRatePercent">The occupancy tax, in percent of the nights price after the discount.</param>
public sealed record FeeRates(decimal GuestServiceFeePercent, decimal TaxRatePercent)
{
    /// <summary>
    /// No service fee and no taxes.
    /// </summary>
    public static readonly FeeRates None = new(0, 0);
}
