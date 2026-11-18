using Lodge.Application.Apartments.GetById;
using Lodge.Application.Hosting.Listings;
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
/// Gets a listed apartment. Unlisted listings are only returned to their host (with the private fields of
/// <see cref="HostListingResponse"/>); everyone else gets 404.
/// </summary>
internal sealed class GetById : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("apartments/{apartmentId:guid}", async (
            Guid apartmentId,
            HttpContext httpContext,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<ApartmentResponse> result = await sender.Send(new GetApartmentByIdQuery(apartmentId), cancellationToken);

            if (result.IsFailure && httpContext.User.Identity?.IsAuthenticated == true)
            {
                Result<HostListingResponse> ownListing =
                    await sender.Send(new GetHostListingQuery(apartmentId), cancellationToken);

                if (ownListing.IsSuccess)
                {
                    return Results.Ok(ownListing.Value);
                }
            }

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Apartments);
    }
}
