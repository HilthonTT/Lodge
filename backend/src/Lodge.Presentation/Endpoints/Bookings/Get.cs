using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Bookings.Get;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Bookings;

/// <summary>
/// Represents the (admin-only) endpoint for listing every user's bookings.
/// </summary>
internal sealed class Get : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("bookings", async (
            [FromQuery] int? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new GetBookingsQuery(
                status,
                page ?? Paging.DefaultPage,
                pageSize ?? Paging.DefaultPageSize);

            Result<PagedList<BookingResponse>> result = await sender.Send(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Bookings)
        .RequireAuthorization(AuthorizationPolicies.Admin);
    }
}
