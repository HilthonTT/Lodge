namespace Lodge.Contracts.Apartments;

/// <summary>
/// A destination suggestion for the search box.
/// </summary>
/// <param name="Label">The display label, e.g. <c>Rovaniemi, Finland</c>.</param>
/// <param name="City">The city.</param>
/// <param name="Country">The country.</param>
/// <param name="Latitude">The average latitude of the listings there.</param>
/// <param name="Longitude">The average longitude of the listings there.</param>
/// <param name="ListingCount">The number of listed apartments there.</param>
public sealed record LocationSuggestionResponse(
    string Label,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    int ListingCount);
