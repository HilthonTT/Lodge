namespace Lodge.Contracts.Payments;

/// <summary>
/// Static exchange rates for displaying prices in other currencies (<c>GET /currencies</c>). Charges always stay in
/// the listing's currency.
/// </summary>
/// <param name="Base">The base currency (<c>USD</c>).</param>
/// <param name="Rates">Units of each currency per 1 unit of <paramref name="Base"/> (the base itself is 1).</param>
/// <param name="UpdatedOnUtc">When the rates were last updated.</param>
public sealed record CurrenciesResponse(string Base, IReadOnlyDictionary<string, decimal> Rates, DateTime UpdatedOnUtc);
