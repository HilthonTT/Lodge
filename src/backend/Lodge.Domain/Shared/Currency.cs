namespace Lodge.Domain.Shared;

/// <summary>
/// Represents the currency value object.
/// </summary>
public sealed record Currency
{
    internal static readonly Currency None = new("");

    private static readonly Currency Usd = new("USD");

    private static readonly Currency Eur = new("EUR");

    private Currency(string code) => Code = code;

    public string Code { get; init; }

    public static readonly IReadOnlyCollection<Currency> All = [Usd, Eur];

    public static Currency FromCode(string code) =>
        All.FirstOrDefault(c => c.Code == code) ??
               throw new ApplicationException("The currency code is invalid");
    
}
