using System.ComponentModel.DataAnnotations;
using Lodge.Domain.Bookings;

namespace Lodge.Application.Payments;

/// <summary>
/// Payment settings (section <c>Payments</c>).
/// </summary>
public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>
    /// Gets or sets <c>Stripe</c> or <c>Simulated</c>. When empty, the mode is <c>Stripe</c> if <c>Stripe:SecretKey</c>
    /// is set, otherwise <c>Simulated</c> in the Development environment and <c>Stripe</c> (not configured) elsewhere:
    /// outside Development, simulated payments must be enabled explicitly.
    /// </summary>
    public string? Mode { get; set; }

    /// <summary>
    /// Gets or sets the guest service fee in percent of the stay subtotal. Defaults to 12.
    /// </summary>
    [Range(typeof(decimal), "0", "100")]
    public decimal GuestServiceFeePercent { get; set; } = 12;

    /// <summary>
    /// Gets or sets the host service fee in percent of the stay subtotal, deducted from payouts. Defaults to 3.
    /// </summary>
    [Range(typeof(decimal), "0", "100")]
    public decimal HostServiceFeePercent { get; set; } = 3;

    /// <summary>
    /// Gets or sets the occupancy tax rates in percent by listing country (the <c>Address.Country</c> of the listing,
    /// case-insensitive), e.g. <c>Payments:TaxRates:Netherlands = 12.5</c>. Configured entries are added to (or
    /// override) these defaults.
    /// </summary>
    public Dictionary<string, decimal> TaxRates { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Netherlands"] = 12.5m,
        ["France"] = 5m,
        ["United States"] = 10m,
        ["Portugal"] = 4m,
        ["Finland"] = 10m,
        ["New Zealand"] = 0m,
        ["United Kingdom"] = 0m,
    };

    /// <summary>
    /// Gets or sets the occupancy tax rate of countries without an entry in <see cref="TaxRates"/>. Defaults to 0.
    /// </summary>
    [Range(typeof(decimal), "0", "100")]
    public decimal DefaultTaxRatePercent { get; set; }

    /// <summary>
    /// Gets the occupancy tax rate of a country.
    /// </summary>
    public decimal TaxRateFor(string? country)
    {
        if (!string.IsNullOrWhiteSpace(country))
        {
            foreach ((string name, decimal rate) in TaxRates)
            {
                if (string.Equals(name.Trim(), country.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return rate;
                }
            }
        }

        return DefaultTaxRatePercent;
    }

    /// <summary>
    /// Gets the fee rates of a stay in <paramref name="country"/>.
    /// </summary>
    public FeeRates FeeRatesFor(string? country) => new(GuestServiceFeePercent, TaxRateFor(country));

    internal bool HasValidMode() =>
        string.IsNullOrWhiteSpace(Mode) || Enum.TryParse<PaymentMode>(Mode.Trim(), ignoreCase: true, out _);
}
