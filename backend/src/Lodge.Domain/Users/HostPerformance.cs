namespace Lodge.Domain.Users;

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
