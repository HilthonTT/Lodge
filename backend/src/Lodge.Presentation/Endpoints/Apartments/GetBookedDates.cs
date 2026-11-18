using Lodge.Application.Apartments.GetDisabledDates;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Apartments;

/// <summary>
/// Represents the endpoint for fetching the booked dates of an apartment.
/// </summary>
internal sealed class GetBookedDates : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("apartments/{apartmentId}/booked-dates", async (
            Guid apartmentId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new GetApartmentDisabledDatesQuery(apartmentId);

            Result<List<DateTime>> result = await sender.Send(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Apartments);
    }
}
