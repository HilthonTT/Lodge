namespace Lodge.Contracts.Reviews;

/// <summary>
/// The category ratings of a review (each 1 to 5).
/// </summary>
public sealed record CategoryRatingsDto
{
    public int Cleanliness { get; init; }

    public int Accuracy { get; init; }

    public int CheckIn { get; init; }

    public int Communication { get; init; }

    public int Location { get; init; }

    public int Value { get; init; }
}
