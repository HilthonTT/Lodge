using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;

namespace Lodge.Application.Apartments.Search;

/// <summary>
/// Searches the listed apartments. Every filter is optional and filters combine with AND.
/// </summary>
public sealed record SearchApartmentQuery : IQuery<PagedList<ApartmentResponse>>
{
    /// <summary>
    /// Gets a case- and accent-insensitive "contains" match on the name, city, state or country.
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Gets the sort column: <c>name</c> (default), <c>rating</c>, <c>price_amount</c>, <c>country</c>,
    /// <c>description</c>, <c>currency</c> or <c>newest</c>.
    /// </summary>
    public string? SortColumn { get; init; }

    /// <summary>
    /// Gets <c>asc</c> (default) or <c>desc</c>.
    /// </summary>
    public string? SortOrder { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    /// <summary>
    /// Gets the check-in date; requires <see cref="EndDate"/>. Only apartments free for the whole stay (and whose
    /// minimum/maximum stay allows its length) are returned.
    /// </summary>
    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public int? GuestCount { get; init; }

    public int? Bedrooms { get; init; }

    public int? Beds { get; init; }

    public decimal? Bathrooms { get; init; }

    /// <summary>
    /// Gets the minimum base nightly price, in the base currency (USD): listing prices are converted with the static
    /// <c>Currencies:Rates</c> before comparing.
    /// </summary>
    public decimal? MinPrice { get; init; }

    /// <summary>
    /// Gets the maximum base nightly price, in the base currency (USD; see <see cref="MinPrice"/>).
    /// </summary>
    public decimal? MaxPrice { get; init; }

    /// <summary>
    /// Gets amenity names; apartments must have all of them.
    /// </summary>
    public IReadOnlyList<string> Amenities { get; init; } = [];

    /// <summary>
    /// Gets property type names; apartments must have one of them.
    /// </summary>
    public IReadOnlyList<string> PropertyTypes { get; init; } = [];

    public bool? InstantBook { get; init; }

    /// <summary>
    /// Gets a value indicating whether only (true) or no (false) Superhost listings are returned.
    /// </summary>
    public bool? Superhost { get; init; }

    public double? MinLatitude { get; init; }

    public double? MaxLatitude { get; init; }

    /// <summary>
    /// Gets the western longitude bound. When it is greater than <see cref="MaxLongitude"/>, the box crosses the
    /// antimeridian.
    /// </summary>
    public double? MinLongitude { get; init; }

    public double? MaxLongitude { get; init; }

    /// <summary>
    /// Gets <c>weekend</c>, <c>week</c> or <c>month</c>; excludes exact dates.
    /// </summary>
    public string? Flexible { get; init; }

    /// <summary>
    /// Gets the months (<c>yyyy-MM</c>, at most 12) a flexible stay may start in; defaults to the current month and the
    /// next two.
    /// </summary>
    public IReadOnlyList<string> Months { get; init; } = [];
}
