namespace Lodge.Contracts.Experiences;

/// <summary>
/// An experience booking (guest and host views).
/// </summary>
public sealed record ExperienceBookingResponse
{
    public Guid Id { get; init; }

    public Guid ExperienceId { get; init; }

    public string ExperienceTitle { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string? ImageUrl { get; init; }

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public string MeetingPoint { get; init; } = string.Empty;

    public int DurationMinutes { get; init; }

    public Guid SessionId { get; init; }

    public DateTime StartsAtUtc { get; init; }

    public Guid HostId { get; init; }

    public string HostFirstName { get; init; } = string.Empty;

    public Guid GuestId { get; init; }

    public string GuestName { get; init; } = string.Empty;

    public int GuestCount { get; init; }

    /// <summary>
    /// Gets the price per guest × guests.
    /// </summary>
    public decimal PriceTotal { get; init; }

    public decimal ServiceFee { get; init; }

    /// <summary>
    /// Gets the amount charged: <see cref="PriceTotal"/> + <see cref="ServiceFee"/>.
    /// </summary>
    public decimal TotalPrice { get; init; }

    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// Gets <c>Reserved</c>, <c>Confirmed</c>, <c>Cancelled</c> or <c>Completed</c>.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    public decimal? AmountPaid { get; init; }

    public DateTime? PaidOnUtc { get; init; }

    public decimal? RefundAmount { get; init; }

    /// <summary>
    /// Gets <c>None</c>, <c>Pending</c>, <c>Succeeded</c>, <c>Failed</c> or <c>Manual</c>.
    /// </summary>
    public string RefundStatus { get; init; } = "None";

    public bool CancelledByHost { get; init; }

    public DateTime? CancelledOnUtc { get; init; }

    /// <summary>
    /// Gets the last moment for a full refund (start − 24 h).
    /// </summary>
    public DateTime FreeCancellationUntilUtc { get; init; }

    public bool CanCancel { get; init; }

    public bool CanReview { get; init; }

    public bool HasReview { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}
