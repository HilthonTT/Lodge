namespace Lodge.Contracts.Experiences;

/// <summary>
/// An experience in search results.
/// </summary>
public sealed record ExperienceSummaryResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public int DurationMinutes { get; init; }

    public decimal PricePerGuest { get; init; }

    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// Gets the cover photo URL, or null (the frontend shows a category placeholder).
    /// </summary>
    public string? ImageUrl { get; init; }

    public IReadOnlyList<string> Languages { get; init; } = [];

    public decimal? AverageRating { get; init; }

    public int ReviewCount { get; init; }

    /// <summary>
    /// Gets the start of the next session with enough spots (on the searched date, if any).
    /// </summary>
    public DateTime? NextSessionStartsAtUtc { get; init; }

    public Guid HostId { get; init; }

    public string HostFirstName { get; init; } = string.Empty;
}
