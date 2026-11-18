namespace Lodge.Contracts.Experiences;

/// <summary>
/// Creates or updates an experience (<c>POST</c>/<c>PUT /hosting/experiences</c>).
/// </summary>
public sealed record ExperienceRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets <c>Food</c>, <c>Outdoors</c>, <c>Art</c>, <c>Culture</c>, <c>Sports</c>, <c>Wellness</c>, <c>Nightlife</c>
    /// or <c>Workshops</c>.
    /// </summary>
    public string Category { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string MeetingPoint { get; init; } = string.Empty;

    public int DurationMinutes { get; init; }

    public decimal PricePerGuest { get; init; }

    /// <summary>
    /// Gets <c>USD</c> or <c>EUR</c>.
    /// </summary>
    public string Currency { get; init; } = "USD";

    public int MaxGuests { get; init; }

    /// <summary>
    /// Gets the ISO 639-1 language codes (e.g. <c>["en", "nl"]</c>).
    /// </summary>
    public IReadOnlyList<string> Languages { get; init; } = [];

    public IReadOnlyList<string> Included { get; init; } = [];

    /// <summary>
    /// Gets the photo ids (from <c>POST /hosting/photos</c>), cover photo first; 0 to 20.
    /// </summary>
    public IReadOnlyList<Guid> ImageIds { get; init; } = [];

    /// <summary>
    /// Gets whether the experience is listed (create only; defaults to true).
    /// </summary>
    public bool? IsListed { get; init; }
}
