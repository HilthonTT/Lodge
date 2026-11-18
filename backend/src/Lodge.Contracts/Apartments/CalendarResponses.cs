namespace Lodge.Contracts.Apartments;

/// <summary>
/// One night of a listing's public calendar.
/// </summary>
/// <param name="Date">The date the night starts on.</param>
/// <param name="Available">Whether the night can be booked (not booked, not blocked, not in the past).</param>
/// <param name="Price">The nightly price (override, weekend or base price).</param>
public sealed record ApartmentCalendarDayResponse(DateOnly Date, bool Available, decimal Price);

/// <summary>
/// One night of a listing's calendar as the host sees it.
/// </summary>
/// <param name="Date">The date the night starts on.</param>
/// <param name="Available">Whether guests can book the night (not booked, not blocked, not in the past).</param>
/// <param name="Blocked">Whether the host blocked the night.</param>
/// <param name="Booked">Whether an active booking or booking request holds the night.</param>
/// <param name="Price">The nightly price (override, weekend or base price).</param>
/// <param name="CustomPrice">Whether <paramref name="Price"/> is a per-night override.</param>
public sealed record HostCalendarDayResponse(
    DateOnly Date,
    bool Available,
    bool Blocked,
    bool Booked,
    decimal Price,
    bool CustomPrice);

/// <summary>
/// The range of base nightly prices over the listed apartments.
/// </summary>
public sealed record PriceRangeResponse(decimal Min, decimal Max, string Currency);

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
