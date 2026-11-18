namespace Lodge.Contracts.Hosting;

/// <summary>
/// A row of the host's listings table.
/// </summary>
public sealed class HostListingSummaryResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string PropertyType { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public decimal Price { get; init; }

    public string Currency { get; init; } = string.Empty;

    public bool IsListed { get; init; }

    public bool InstantBook { get; init; }

    public decimal? AverageRating { get; init; }

    public int ReviewCount { get; init; }

    /// <summary>
    /// Gets the number of reserved or confirmed stays that have not ended yet.
    /// </summary>
    public int UpcomingBookingCount { get; init; }

    /// <summary>
    /// Gets the number of booking requests waiting for the host's answer.
    /// </summary>
    public int PendingRequestCount { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public DateTime? ModifiedOnUtc { get; init; }
}

/// <summary>
/// The hosting dashboard numbers (<c>GET /hosting/summary</c>). Earnings come from the payouts (after the host fee):
/// <paramref name="EarningsThisMonth"/> = scheduled, on hold and paid payouts dated this calendar month (UTC),
/// <paramref name="PaidTotal"/> = all paid payouts, <paramref name="UpcomingPayouts"/> = scheduled and on hold payouts.
/// </summary>
public sealed record HostingSummaryResponse(
    int ListingCount,
    int UpcomingStays,
    int PendingRequests,
    decimal EarningsThisMonth,
    decimal PaidTotal,
    decimal UpcomingPayouts,
    string Currency,
    decimal? AverageRating,
    int ReviewCount,
    bool IsSuperhost,
    SuperhostProgressResponse Superhost);

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

/// <summary>
/// The Superhost requirements; the cancellation rate must be strictly below <paramref name="MaxCancellationRate"/>.
/// </summary>
public sealed record SuperhostRequirementsResponse(
    int MinCompletedStays,
    decimal MinAverageRating,
    decimal MaxCancellationRate);
