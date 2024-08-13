using Lodge.Application.Stripe.Checkout;
using Lodge.Contracts.Stripe;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Stripe;

/// <summary>
/// Represents the stripe endpoint for checking out your booking
/// </summary>
internal sealed class Checkout : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("stripe/checkout", async (
            StripeCheckoutRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new StripeCheckoutCommand(request.UserId, request.BookingId);

            Result<string> result = await sender.Send(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Stripe);
    }
}
