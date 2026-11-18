namespace Lodge.Contracts.Experiences;

/// <summary>
/// The full experience (<c>GET /experiences/{id}</c> and <c>GET /hosting/experiences/{id}</c>).
/// </summary>
public sealed record ExperienceResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string MeetingPoint { get; init; } = string.Empty;

    public int DurationMinutes { get; init; }

    public decimal PricePerGuest { get; init; }

    public string Currency { get; init; } = string.Empty;

    public int MaxGuests { get; init; }

    public IReadOnlyList<string> Languages { get; init; } = [];

    public IReadOnlyList<string> Included { get; init; } = [];

    public IReadOnlyList<ExperienceImageResponse> Images { get; init; } = [];

    public ExperienceHostResponse Host { get; init; } = null!;

    public decimal? AverageRating { get; init; }

    public int ReviewCount { get; init; }

    public bool IsListed { get; init; }

    /// <summary>
    /// Gets how many hours before the start guests can cancel with a full refund (24).
    /// </summary>
    public int FreeCancellationHours { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}
