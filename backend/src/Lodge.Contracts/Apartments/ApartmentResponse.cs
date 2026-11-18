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

    public int MinNights { get; init; }

    public int MaxNights { get; init; }

    /// <summary>
    /// Gets or sets the average review rating, rounded to one decimal; <c>null</c> when there are no reviews.
    /// </summary>
    public decimal? AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public AddressResponse Address { get; set; } = new();

    public HostSummaryResponse Host { get; set; } = null!;
}

/// <summary>
/// A listing as its host sees it: the public fields plus the private and management ones.
/// </summary>
public sealed class HostListingResponse : ApartmentResponse
{
    public string? CheckInInstructions { get; init; }

    public bool IsListed { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public DateTime? ModifiedOnUtc { get; init; }
}

/// <summary>
/// A photo of a listing.
/// </summary>
/// <param name="Id">The photo identifier (for uploaded photos, the file identifier).</param>
/// <param name="Url">The absolute URL.</param>
public sealed record ApartmentImageResponse(Guid Id, string Url);

/// <summary>
/// The host of a listing.
/// </summary>
public sealed record HostSummaryResponse(
    Guid Id,
    string FirstName,
    string? AvatarUrl,
    bool IsSuperhost,
    DateTime JoinedOnUtc);
