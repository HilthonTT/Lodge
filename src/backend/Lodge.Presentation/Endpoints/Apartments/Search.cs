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
            [FromQuery] string? searchTerm,
            [FromQuery] string? sortColumn,
            [FromQuery] string? sortOrder,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new SearchApartmentQuery(searchTerm, sortColumn, sortOrder, page, pageSize, startDate, endDate);

            Result<PagedList<ApartmentResponse>> result = await sender.Send(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Apartments);
    }
}
