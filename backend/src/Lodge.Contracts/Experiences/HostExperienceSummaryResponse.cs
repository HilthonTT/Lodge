namespace Lodge.Contracts.Experiences;

/// <summary>
/// The host's experience list item (<c>GET /hosting/experiences</c>).
/// </summary>
public sealed record HostExperienceSummaryResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public decimal PricePerGuest { get; init; }

    public string Currency { get; init; } = string.Empty;

    public bool IsListed { get; init; }

    public string? ImageUrl { get; init; }

    public int UpcomingSessionCount { get; init; }

    public decimal? AverageRating { get; init; }

    public int ReviewCount { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}
