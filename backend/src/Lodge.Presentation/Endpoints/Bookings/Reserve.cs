using Lodge.Application.Bookings.Reserve;
using Lodge.Contracts.Bookings;
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
/// Represents the endpoint for reserving a booking.
/// </summary>
internal sealed class Reserve : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("bookings", async (
            [FromHeader(Name = IdempotencyKey.HeaderName)] string? idempotencyKey,
            ReserveBookingRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (!IdempotencyKey.TryParse(idempotencyKey, out Guid requestId))
            {
                return IdempotencyKey.Invalid();
            }

            var command = new ReserveBookingCommand(
                requestId, 
                request.ApartmentId, 
                request.UserId,
                request.StartDate,
                request.EndDate,
                request.GuestCount,
                request.MessageToHost);

            Result<Guid> result = await sender.Send(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Bookings)
        .RequireAuthorization();
    }
}
