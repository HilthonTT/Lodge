using Lodge.Application.Bookings.GetById;
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
/// Represents the endpoint for fetching a booking by its identifier.
/// </summary>
internal sealed class GetById : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("bookings/{bookingId}", async (
            Guid bookingId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new GetBookingByIdQuery(bookingId);

            Result<BookingResponse> result = await sender.Send(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Bookings)
        .RequireAuthorization();
    }
}
