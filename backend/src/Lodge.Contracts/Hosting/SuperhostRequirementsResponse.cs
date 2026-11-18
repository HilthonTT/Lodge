namespace Lodge.Contracts.Hosting;

/// <summary>
/// The Superhost requirements; the cancellation rate must be strictly below <paramref name="MaxCancellationRate"/>.
/// </summary>
public sealed record SuperhostRequirementsResponse(
    int MinCompletedStays,
    decimal MinAverageRating,
    decimal MaxCancellationRate);
