namespace Lodge.Contracts.Experiences;

/// <summary>
/// A session as the host sees it (<c>GET /hosting/experiences/{id}/sessions</c>).
/// </summary>
public sealed record HostExperienceSessionResponse
{
    public Guid Id { get; init; }

    public DateTime StartsAtUtc { get; init; }

    public int Capacity { get; init; }

    public int BookedGuests { get; init; }

    public int SpotsLeft { get; init; }

    /// <summary>
    /// Gets the number of reserved and confirmed bookings.
    /// </summary>
    public int BookingCount { get; init; }

    public bool IsCancelled { get; init; }
}
