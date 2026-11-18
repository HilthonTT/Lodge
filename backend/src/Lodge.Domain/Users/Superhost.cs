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

/// <summary>
/// A host's track record across all of their listings (deleted ones included).
/// </summary>
/// <param name="CompletedStays">The number of completed stays.</param>
/// <param name="AverageRating">The average review rating, or null without reviews.</param>
/// <param name="ReviewCount">The number of reviews.</param>
/// <param name="HostCancellations">
/// The number of bookings ended by the host side: declined requests, rejected bookings (status Rejected) and
/// confirmed bookings the host cancelled. Guest cancellations never count.
/// </param>
/// <param name="TotalBookings">
/// The number of bookings the host answered: accepted ones (instant bookings and accepted requests, whatever happened
/// to them afterwards, guest cancellations included) and declined or rejected ones. Pending requests, expired requests
/// and requests the guest cancelled before the host answered are not counted.
/// </param>
public sealed record HostPerformance(
    int CompletedStays,
    decimal? AverageRating,
    int ReviewCount,
    int HostCancellations,
    int TotalBookings)
{
    public static readonly HostPerformance None = new(0, null, 0, 0, 0);

    /// <summary>
    /// Gets <see cref="HostCancellations"/> / <see cref="TotalBookings"/> (0 without bookings), rounded to 4 decimals.
    /// </summary>
    public decimal CancellationRate =>
        TotalBookings == 0 ? 0 : Math.Round((decimal)HostCancellations / TotalBookings, 4, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Checks the Superhost requirements.
    /// </summary>
    public bool Meets(SuperhostRequirements requirements)
    {
        ArgumentNullException.ThrowIfNull(requirements);

        return CompletedStays >= requirements.MinCompletedStays &&
               AverageRating is decimal rating && rating >= requirements.MinAverageRating &&
               CancellationRate < requirements.MaxCancellationRate;
    }
}
