using Microsoft.AspNetCore.Mvc;

namespace Lodge.Presentation.Endpoints.Apartments;

/// <summary>
/// The query string of <c>GET /apartments/search</c>.
/// </summary>
internal sealed class SearchApartmentsParameters
{
    [FromQuery] public string? SearchTerm { get; init; }

    [FromQuery] public string? SortColumn { get; init; }

    [FromQuery] public string? SortOrder { get; init; }

    [FromQuery] public int? Page { get; init; }

    [FromQuery] public int? PageSize { get; init; }

    [FromQuery] public DateOnly? StartDate { get; init; }

    [FromQuery] public DateOnly? EndDate { get; init; }

    [FromQuery] public int? GuestCount { get; init; }

    [FromQuery] public int? Bedrooms { get; init; }

    [FromQuery] public int? Beds { get; init; }

    [FromQuery] public decimal? Bathrooms { get; init; }

    [FromQuery] public decimal? MinPrice { get; init; }

    [FromQuery] public decimal? MaxPrice { get; init; }

    [FromQuery] public string[]? Amenities { get; init; }

    [FromQuery] public string[]? PropertyTypes { get; init; }

    [FromQuery] public bool? InstantBook { get; init; }

    [FromQuery] public bool? Superhost { get; init; }

    [FromQuery] public double? MinLat { get; init; }

    [FromQuery] public double? MaxLat { get; init; }

    [FromQuery] public double? MinLng { get; init; }

    [FromQuery] public double? MaxLng { get; init; }

    [FromQuery] public string? Flexible { get; init; }

    [FromQuery] public string[]? Months { get; init; }
}
