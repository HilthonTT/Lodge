using Lodge.Application.Apartments.GetById;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Apartments;

/// <summary>
/// Represents the endpoint for fetching the apartment by its identifier.
/// </summary>
internal sealed class GetById : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("apartments/{apartmentId}", async (
            Guid apartmentId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new GetApartmentByIdQuery(apartmentId);

            Result<ApartmentResponse> result = await sender.Send(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Apartments);
    }
}
