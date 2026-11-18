namespace Lodge.Contracts.Apartments;

/// <summary>
/// Represents a listing as guests see it.
/// </summary>
public class ApartmentResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the property type name (e.g. <c>"Cabin"</c>).
    /// </summary>
    public string PropertyType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the base nightly price.
    /// </summary>
    public decimal Price { get; init; }

    public string Currency { get; init; } = string.Empty;

    public decimal CleaningFee { get; init; }

    /// <summary>
    /// Gets the nightly price of Friday and Saturday nights, or null.
    /// </summary>
    public decimal? WeekendPrice { get; init; }

    /// <summary>
    /// Gets the discount (percent) for stays of at least 7 nights.
    /// </summary>
    public int WeeklyDiscountPercent { get; init; }

    /// <summary>
    /// Gets the discount (percent) for stays of at least 28 nights.
    /// </summary>
    public int MonthlyDiscountPercent { get; init; }

    /// <summary>
    /// Gets the absolute URL of the cover photo (the first of <see cref="Images"/>).
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets the photos in display order.
    /// </summary>
    public IReadOnlyList<ApartmentImageResponse> Images { get; set; } = [];

    public int Bedrooms { get; init; }

    public int Beds { get; init; }

    /// <summary>
    /// Gets the number of bathrooms, in steps of 0.5.
    /// </summary>
    public decimal Bathrooms { get; init; }

    public int MaximumGuestCount { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    /// <summary>
    /// Gets the amenity names (e.g. <c>"WiFi"</c>).
    /// </summary>
    public IReadOnlyList<string> Amenities { get; init; } = [];

    /// <summary>
    /// Gets the earliest check-in time, <c>HH:mm</c>.
    /// </summary>
    public string CheckInTime { get; init; } = string.Empty;

    /// <summary>
    /// Gets the latest check-out time, <c>HH:mm</c>.
    /// </summary>
    public string CheckOutTime { get; init; } = string.Empty;

    public bool PetsAllowed { get; init; }

    public bool SmokingAllowed { get; init; }

    public bool PartiesAllowed { get; init; }

    public string? HouseRules { get; init; }

    public bool InstantBook { get; init; }

    /// <summary>
    /// Gets the cancellation policy: <c>Flexible</c>, <c>Moderate</c> or <c>Strict</c>.
    /// </summary>
    public string CancellationPolicy { get; init; } = string.Empty;

    public int MinNights { get; init; }

    public int MaxNights { get; init; }

    /// <summary>
    /// Gets or sets the average review rating, rounded to one decimal; <c>null</c> when there are no reviews.
    /// </summary>
    public decimal? AverageRating { get; set; }

    public int ReviewCount { get; set; }

    /// <summary>
    /// Gets or sets the average category ratings of the published reviews (1 decimal), or null without reviews.
    /// </summary>
    public RatingBreakdownResponse? RatingBreakdown { get; set; }

    public AddressResponse Address { get; set; } = new();

    public HostSummaryResponse Host { get; set; } = null!;
}
