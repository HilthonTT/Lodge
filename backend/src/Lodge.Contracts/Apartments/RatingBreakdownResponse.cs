namespace Lodge.Contracts.Apartments;

/// <summary>
/// The average category ratings of a listing.
/// </summary>
public sealed record RatingBreakdownResponse(
    decimal Cleanliness,
    decimal Accuracy,
    decimal CheckIn,
    decimal Communication,
    decimal Location,
    decimal Value);
