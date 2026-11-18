using Lodge.Application.Stripe.Webhook;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Stripe;

internal sealed class Webhook : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("stripe/webhook", async (
            HttpRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            // The signature is computed over the exact raw body, so it must be read unparsed.
            using var reader = new StreamReader(request.Body);
            string payload = await reader.ReadToEndAsync(cancellationToken);

            var command = new StripeWebhookCommand(payload, request.Headers["Stripe-Signature"]);

            Result result = await sender.Send(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Stripe)
        .AllowAnonymous()
        .ExcludeFromDescription();
    }
}
