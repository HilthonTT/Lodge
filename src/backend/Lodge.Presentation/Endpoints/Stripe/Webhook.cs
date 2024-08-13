using Lodge.Application.Stripe.Webhook;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Stripe;

/// <summary>
/// Represents the stripe webhook endpoint
/// </summary>
internal sealed class Webhook : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("stripe/webhook", async (
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new StripeWebhookCommand();

            Result result = await sender.Send(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Stripe);
    }
}
