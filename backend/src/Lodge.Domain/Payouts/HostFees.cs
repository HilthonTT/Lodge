using Lodge.Domain.Bookings;

namespace Lodge.Domain.Payouts;

/// <summary>
/// Calculates what hosts are paid.
/// </summary>
public static class HostFees
{
    /// <summary>
    /// Gets the host fee: <paramref name="feePercent"/> percent of <paramref name="stayAmount"/>, rounded to cents.
    /// </summary>
    public static decimal Fee(decimal stayAmount, decimal feePercent) =>
        PricingService.Round(stayAmount * feePercent / 100m);

    /// <summary>
    /// Gets the payout for a stay amount: the amount minus the host fee.
    /// </summary>
    public static decimal Payout(decimal stayAmount, decimal feePercent) => stayAmount - Fee(stayAmount, feePercent);
}
