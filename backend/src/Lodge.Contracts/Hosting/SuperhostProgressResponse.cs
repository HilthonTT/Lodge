namespace Lodge.Contracts.Hosting;

/// <summary>
/// The host's progress towards the Superhost requirements.
/// </summary>
/// <param name="CompletedStays">Completed stays across the host's listings.</param>
/// <param name="AverageRating">The average rating across the host's listings (2 decimals), or null.</param>
/// <param name="CancellationRate">
/// Host-side cancellations (declined/rejected requests and host-cancelled bookings) / bookings, between 0 and 1.
/// </param>
/// <param name="MeetsRequirements">Whether the host currently meets every requirement.</param>
/// <param name="Requirements">The requirements.</param>
public sealed record SuperhostProgressResponse(
    int CompletedStays,
    decimal? AverageRating,
    decimal CancellationRate,
    bool MeetsRequirements,
    SuperhostRequirementsResponse Requirements);
