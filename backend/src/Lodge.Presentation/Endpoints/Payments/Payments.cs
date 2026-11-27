using Lodge.Application.Payments;
using Lodge.Application.Payments.CompleteSimulated;
using Lodge.Application.Payments.GetConfig;
using Lodge.Application.Payments.GetCurrencies;
using Lodge.Contracts.Payments;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Payments;

/// <summary>
/// The payment mode, the simulated checkout and the display exchange rates.
/// </summary>
internal sealed class Payments : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("payments/config", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<PaymentsConfigResponse> result = await sender.Send(new GetPaymentsConfigQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Payments)
        .AllowAnonymous();

        app.MapPost("payments/simulated/{bookingId:guid}/complete", async (
            Guid bookingId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new CompleteSimulatedPaymentCommand(bookingId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Payments)
        .RequireAuthorization();

        app.MapGet("currencies", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<CurrenciesResponse> result = await sender.Send(new GetCurrenciesQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Payments)
        .AllowAnonymous();
    }
}
