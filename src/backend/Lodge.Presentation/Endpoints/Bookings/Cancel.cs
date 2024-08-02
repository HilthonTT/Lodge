using Lodge.Application.Bookings.Cancel;
using Lodge.Contracts.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Bookings;

/// <summary>
/// Represents the endpoint for cancelling a booking.
/// </summary>
internal sealed class Cancel : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("bookings/{bookingId}/cancel", async (
            Guid bookingId,
            CancelBookingRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new CancelBookingCommand(request.UserId, bookingId);

            Result result = await sender.Send(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Bookings);
    }
}
