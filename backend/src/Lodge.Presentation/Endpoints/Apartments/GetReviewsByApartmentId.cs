using Lodge.Application.Reviews.GetByApartmentId;
using Lodge.Contracts.Common;
using Lodge.Contracts.Reviews;
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
/// Represents the endpoint for fetching the reviews based on their apartment identifier.
/// </summary>
internal sealed class GetReviewsByApartmentId : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("apartments/{apartmentId}/reviews", async (
            Guid apartmentId,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new GetReviewsByApartmentIdQuery(apartmentId, page, pageSize);

            Result<PagedList<ReviewResponse>> result = await sender.Send(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Apartments);
    }
}
