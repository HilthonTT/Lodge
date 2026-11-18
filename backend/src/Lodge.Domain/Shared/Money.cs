namespace Lodge.Domain.Shared;

/// <summary>
/// Represents the money value object.
/// </summary>
/// <param name="Amount">The amount.</param>
/// <param name="Currency">The currency type.</param>
public sealed record Money(decimal Amount, Currency Currency)
{
    public static Money operator +(Money first, Money second)
    {
        if (first.Currency != second.Currency)
        {
            throw new InvalidOperationException("Currencies must be equal");
        }

        return new Money(first.Amount + second.Amount, first.Currency);
    }

    public static Money Zero() => new(0, Currency.None);

    public static Money Zero(Currency currency) => new(0, currency);

    public bool IsZero() => this == Zero(Currency);
}
