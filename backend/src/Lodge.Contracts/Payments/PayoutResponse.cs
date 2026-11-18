namespace Lodge.Contracts.Payments;

/// <summary>
/// A payout to the host (<c>GET /hosting/payouts</c>).
/// </summary>
public sealed class PayoutResponse
{
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the paid stay, or null for an experience payout.
    /// </summary>
    public Guid? BookingId { get; init; }

    /// <summary>
    /// Gets the paid experience booking, or null for a stay payout.
    /// </summary>
    public Guid? ExperienceBookingId { get; init; }

    /// <summary>
    /// Gets the listing name, or the experience title.
    /// </summary>
    public string ListingName { get; init; } = string.Empty;

    public string GuestName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// Gets <c>Scheduled</c>, <c>OnHold</c>, <c>Paid</c>, <c>Failed</c> or <c>Cancelled</c>.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    public DateOnly ScheduledFor { get; init; }

    public DateTime? PaidOnUtc { get; init; }
}
