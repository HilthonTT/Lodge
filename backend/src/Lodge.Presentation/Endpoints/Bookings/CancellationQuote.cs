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
/// What the guest would get back by cancelling today.
/// </summary>
internal sealed class CancellationQuote : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("bookings/{bookingId:guid}/cancellation-quote", async (
            Guid bookingId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<CancellationQuoteResponse> result =
                await sender.Send(new GetCancellationQuoteQuery(bookingId), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Bookings)
        .RequireAuthorization();
    }
}
