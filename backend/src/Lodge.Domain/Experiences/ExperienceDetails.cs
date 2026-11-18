namespace Lodge.Domain.Experiences;

/// <summary>
/// The editable details of an <see cref="Experience"/>.
/// </summary>
public sealed record ExperienceDetails
{
    public required string Title { get; init; }

    public required string Description { get; init; }

    public required ExperienceCategory Category { get; init; }

    public required string City { get; init; }

    public required string Country { get; init; }

    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    public required string MeetingPoint { get; init; }

    public required int DurationMinutes { get; init; }

    public required decimal PricePerGuest { get; init; }

    /// <summary>
    /// Gets the currency code (<c>USD</c> or <c>EUR</c>).
    /// </summary>
    public required string Currency { get; init; }

    public required int MaxGuests { get; init; }

    /// <summary>
    /// Gets the ISO 639-1 codes of the languages the host speaks during the experience.
    /// </summary>
    public required IReadOnlyList<string> Languages { get; init; }

    public required IReadOnlyList<string> Included { get; init; }
}
