using System.ComponentModel.DataAnnotations;

namespace Lodge.Application.Payments;

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
