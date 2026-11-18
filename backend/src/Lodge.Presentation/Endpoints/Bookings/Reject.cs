using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Bookings.Reject;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Bookings;

/// <summary>
/// Represents the (admin-only) endpoint for rejecting a booking. No request body is needed.
/// </summary>
internal sealed class Reject : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("bookings/{bookingId}/reject", async (
            Guid bookingId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new RejectBookingCommand(bookingId);

            Result result = await sender.Send(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Bookings)
        .RequireAuthorization(AuthorizationPolicies.Admin);
    }
}
