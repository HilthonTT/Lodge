namespace Lodge.Contracts.Hosting;

/// <summary>
/// Creates or updates a listing (<c>POST /hosting/listings</c>, <c>PUT /hosting/listings/{id}</c>).
/// </summary>
public sealed record ListingRequest
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the property type name (case-insensitive), e.g. <c>"Cabin"</c>.
    /// </summary>
    public string PropertyType { get; init; } = string.Empty;

    public ListingAddressRequest Address { get; init; } = new();

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    /// <summary>
    /// Gets the base nightly price.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Gets the currency code (<c>USD</c> or <c>EUR</c>).
    /// </summary>
    public string Currency { get; init; } = "USD";

    public decimal CleaningFee { get; init; }

    public decimal? WeekendPrice { get; init; }

    public int WeeklyDiscountPercent { get; init; }

    public int MonthlyDiscountPercent { get; init; }

    public int Bedrooms { get; init; }

    public int Beds { get; init; }

    public decimal Bathrooms { get; init; }

    public int MaximumGuestCount { get; init; }

    /// <summary>
    /// Gets the amenity names (case-insensitive), e.g. <c>["WiFi", "Kitchen"]</c>.
    /// </summary>
    public IReadOnlyList<string> Amenities { get; init; } = [];

    /// <summary>
    /// Gets the check-in time, <c>HH:mm</c>.
    /// </summary>
    public string CheckInTime { get; init; } = "15:00";

    /// <summary>
    /// Gets the check-out time, <c>HH:mm</c>.
    /// </summary>
    public string CheckOutTime { get; init; } = "11:00";

    public bool PetsAllowed { get; init; }

    public bool SmokingAllowed { get; init; }

    public bool PartiesAllowed { get; init; }

    public string? HouseRules { get; init; }

    public string? CheckInInstructions { get; init; }

    public bool InstantBook { get; init; } = true;

    /// <summary>
    /// Gets the cancellation policy name (case-insensitive): <c>Flexible</c>, <c>Moderate</c> (default) or
    /// <c>Strict</c>.
    /// </summary>
    public string CancellationPolicy { get; init; } = "Moderate";

    public int MinNights { get; init; } = 1;

    public int MaxNights { get; init; } = 365;

    /// <summary>
    /// Gets the photo identifiers in display order (1 to 20): files returned by <c>POST /hosting/photos</c> and/or
    /// the ids of the listing's current photos.
    /// </summary>
    public IReadOnlyList<Guid> ImageIds { get; init; } = [];

    /// <summary>
    /// Gets whether the listing is visible to guests. Defaults to true on create; when omitted on update, the
    /// status is not changed.
    /// </summary>
    public bool? IsListed { get; init; }
}
