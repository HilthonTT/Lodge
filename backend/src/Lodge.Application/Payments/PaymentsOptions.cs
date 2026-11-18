using System.ComponentModel.DataAnnotations;
using Lodge.Domain.Bookings;

namespace Lodge.Application.Payments;

/// <summary>
/// How payments, refunds and payouts are processed.
/// </summary>
public enum PaymentMode
{
    /// <summary>
    /// Stripe Checkout, Refunds and Connect (requires <c>Stripe:SecretKey</c> and <c>Stripe:WebhookSecret</c>).
    /// </summary>
    Stripe,

    /// <summary>
    /// Everything succeeds immediately without calling Stripe (development and demos).
    /// </summary>
    Simulated,
}

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

/// <summary>
/// Static exchange rates for displaying prices (section <c>Currencies</c>). They are not live rates: update them in
/// configuration (e.g. <c>Currencies:Rates:EUR = 0.93</c>) together with <c>Currencies:UpdatedOnUtc</c>.
/// </summary>
public sealed class CurrencyOptions
{
    public const string SectionName = "Currencies";

    /// <summary>
    /// Gets or sets the base currency. Defaults to USD.
    /// </summary>
    [Required]
    public string Base { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the units of each currency per 1 unit of <see cref="Base"/>.
    /// </summary>
    public Dictionary<string, decimal> Rates { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = 1m,
        ["EUR"] = 0.92m,
        ["GBP"] = 0.79m,
        ["JPY"] = 151.50m,
        ["CHF"] = 0.88m,
        ["CAD"] = 1.39m,
        ["AUD"] = 1.53m,
    };

    /// <summary>
    /// Gets or sets when the rates were last updated.
    /// </summary>
    public DateTime UpdatedOnUtc { get; set; } = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
}
