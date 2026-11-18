using Lodge.Domain.Shared;

namespace Lodge.Domain.Apartments;

/// <summary>
/// Everything a host can edit on a listing, except its photos and its listed status.
/// </summary>
public sealed record ListingDetails
{
    public required Name Name { get; init; }

    public required Description Description { get; init; }

    public required PropertyType PropertyType { get; init; }

    public required Address Address { get; init; }

    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    /// <summary>
    /// Gets the base nightly price.
    /// </summary>
    public required Money Price { get; init; }

    public required Money CleaningFee { get; init; }

    /// <summary>
    /// Gets the nightly price for Friday and Saturday nights (same currency as <see cref="Price"/>), or null.
    /// </summary>
    public decimal? WeekendPrice { get; init; }

    public int WeeklyDiscountPercent { get; init; }

    public int MonthlyDiscountPercent { get; init; }

    public required int Bedrooms { get; init; }

    public required int Beds { get; init; }

    public required decimal Bathrooms { get; init; }

    public required int MaximumGuestCount { get; init; }

    public IReadOnlyCollection<Amenity> Amenities { get; init; } = [];

    public TimeOnly CheckInTime { get; init; } = new(15, 0);

    public TimeOnly CheckOutTime { get; init; } = new(11, 0);

    public bool PetsAllowed { get; init; }

    public bool SmokingAllowed { get; init; }

    public bool PartiesAllowed { get; init; }

    public string? HouseRules { get; init; }

    /// <summary>
    /// Gets the private check-in instructions (only shown to the host and to guests with a confirmed booking).
    /// </summary>
    public string? CheckInInstructions { get; init; }

    public bool InstantBook { get; init; } = true;

    public CancellationPolicy CancellationPolicy { get; init; } = CancellationPolicy.Moderate;

    public int MinNights { get; init; } = 1;

    public int MaxNights { get; init; } = Apartment.MaxNightsLimit;
}
