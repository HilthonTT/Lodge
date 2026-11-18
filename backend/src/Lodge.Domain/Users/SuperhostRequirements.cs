namespace Lodge.Domain.Users;

/// <summary>
/// The Superhost requirements.
/// </summary>
/// <param name="MinCompletedStays">The minimum number of completed stays across the host's listings.</param>
/// <param name="MinAverageRating">The minimum average review rating across the host's listings.</param>
/// <param name="MaxCancellationRate">
/// The host's cancellation rate must be strictly below this fraction (e.g. <c>0.10</c> = 10%).
/// </param>
public sealed record SuperhostRequirements(int MinCompletedStays, decimal MinAverageRating, decimal MaxCancellationRate)
{
    public static readonly SuperhostRequirements Default = new(5, 4.8m, 0.10m);
}
