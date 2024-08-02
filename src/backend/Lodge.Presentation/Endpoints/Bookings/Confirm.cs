using Lodge.Application.Bookings.Confirm;
using Lodge.Contracts.Bookings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Bookings;

/// <summary>
/// Represents the endpoint for confirming a booking.
/// </summary>
internal sealed class Confirm : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("bookings/{bookingId}/confirm", async (
            Guid bookingId,
            ConfirmBookingRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            //var command = new ConfirmBookingCommand(request.UserId, bookingId);

            //var result = await sender.Send(request, cancellationToken);

            //return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Bookings);
    }
}
