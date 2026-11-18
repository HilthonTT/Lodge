using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Payments;

/// <summary>
/// Converts amounts to the base currency (USD) with the static rates of <see cref="CurrencyOptions"/>. Used for
/// display-level comparisons only (price range, price filters and price sorting); charges stay in the listing currency.
/// </summary>
public sealed partial class CurrencyConverter(IOptions<CurrencyOptions> options)
{
    /// <summary>
    /// Gets the base currency code.
    /// </summary>
    public string BaseCurrency => options.Value.Base.Trim().ToUpperInvariant();

    /// <summary>
    /// Converts <paramref name="amount"/> in <paramref name="currency"/> to the base currency. Unknown currencies are
    /// treated as the base currency.
    /// </summary>
    public decimal ToBase(decimal amount, string currency) =>
        TryGetRate(currency, out decimal rate) ? amount / rate : amount;

    /// <summary>
    /// Builds a SQL expression converting <paramref name="amountColumn"/> (in <paramref name="currencyColumn"/>) to
    /// the base currency. Only validated three-letter codes and invariant decimal literals from configuration are
    /// embedded, never user input.
    /// </summary>
    public string ToBaseSql(string amountColumn, string currencyColumn)
    {
        var sql = new StringBuilder($"(CASE {currencyColumn}");

        foreach ((string code, decimal rate) in options.Value.Rates)
        {
            string normalized = code.Trim().ToUpperInvariant();

            if (rate > 0 && rate != 1 && normalized != BaseCurrency && CurrencyCode().IsMatch(normalized))
            {
                sql.Append(CultureInfo.InvariantCulture, $" WHEN '{normalized}' THEN {amountColumn} / {rate}");
            }
        }

        sql.Append(CultureInfo.InvariantCulture, $" ELSE {amountColumn} END)");

        return sql.ToString();
    }

    private bool TryGetRate(string currency, out decimal rate)
    {
        foreach ((string code, decimal value) in options.Value.Rates)
        {
            if (value > 0 && string.Equals(code.Trim(), currency.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                rate = value;
                return true;
            }
        }

        rate = 1;
        return false;
    }

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyCode();
}
