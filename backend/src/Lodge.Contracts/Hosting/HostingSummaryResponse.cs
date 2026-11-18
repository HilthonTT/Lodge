namespace Lodge.Contracts.Hosting;

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
