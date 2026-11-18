namespace Lodge.Contracts.Apartments;

/// <summary>
/// The range of base nightly prices over the listed apartments.
/// </summary>
public sealed record PriceRangeResponse(decimal Min, decimal Max, string Currency);
