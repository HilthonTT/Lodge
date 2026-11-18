using Lodge.Application.Apartments.CalculatePrice;
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
/// Represents the endpoint for calculating the total price of an apartment.
/// </summary>
internal sealed class CalculatePrice : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("apartments/{apartmentId}/price", async (
            Guid apartmentId,
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new CalculateApartmentPriceQuery(apartmentId, startDate, endDate);

            Result<PriceDetailsResponse> result = await sender.Send(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Apartments);
    }
}
