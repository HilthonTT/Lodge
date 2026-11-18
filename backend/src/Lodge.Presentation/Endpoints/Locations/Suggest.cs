using Lodge.Application.Locations;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Locations;

/// <summary>
/// Destination autocomplete for the search box. Anonymous.
/// </summary>
internal sealed class Suggest : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("locations/suggest", async (
            [FromQuery] string? q,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<List<LocationSuggestionResponse>> result =
                await sender.Send(new SuggestLocationsQuery(q), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Locations);
    }
}
