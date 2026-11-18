using Lodge.Application.Apartments.Search;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Apartments;

/// <summary>
/// Represents the endpoint for searching apartments.
/// </summary>
internal sealed class Search : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("apartments/search", async (
            [AsParameters] SearchApartmentsParameters parameters,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new SearchApartmentQuery
            {
                SearchTerm = parameters.SearchTerm,
                SortColumn = parameters.SortColumn,
                SortOrder = parameters.SortOrder,
                Page = parameters.Page ?? Paging.DefaultPage,
                PageSize = parameters.PageSize ?? Paging.DefaultPageSize,
                StartDate = parameters.StartDate,
                EndDate = parameters.EndDate,
                GuestCount = parameters.GuestCount,
                Bedrooms = parameters.Bedrooms,
                Beds = parameters.Beds,
                Bathrooms = parameters.Bathrooms,
                MinPrice = parameters.MinPrice,
                MaxPrice = parameters.MaxPrice,
                Amenities = SplitValues(parameters.Amenities),
                PropertyTypes = SplitValues(parameters.PropertyTypes),
                InstantBook = parameters.InstantBook,
                Superhost = parameters.Superhost,
                MinLatitude = parameters.MinLat,
                MaxLatitude = parameters.MaxLat,
                MinLongitude = parameters.MinLng,
                MaxLongitude = parameters.MaxLng,
                Flexible = string.IsNullOrWhiteSpace(parameters.Flexible) ? null : parameters.Flexible,
                Months = SplitValues(parameters.Months),
            };

            Result<PagedList<ApartmentResponse>> result = await sender.Send(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Apartments);
    }

    /// <summary>
    /// Accepts both repeated parameters (<c>?amenities=WiFi&amp;amenities=Kitchen</c>) and comma-separated values.
    /// </summary>
    private static string[] SplitValues(string[]? values) =>
        values is null
            ? []
            : [.. values.SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))];
}

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
