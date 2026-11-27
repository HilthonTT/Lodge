using Lodge.Application.Payouts;
using Lodge.Application.Payouts.Get;
using Lodge.Application.Payouts.GetAccount;
using Lodge.Application.Payouts.StartOnboarding;
using Lodge.Contracts.Common;
using Lodge.Contracts.Payments;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Hosting;

/// <summary>
/// The host's payout account (Stripe Connect Express or simulated) and payouts.
/// </summary>
internal sealed class Payouts : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("hosting/payouts/account", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<PayoutAccountResponse> result = await sender.Send(new GetPayoutAccountQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPost("hosting/payouts/onboarding", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<UrlResponse> result = await sender.Send(new StartPayoutOnboardingCommand(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapGet("hosting/payouts", async (
            [FromQuery] string? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<PagedList<PayoutResponse>> result = await sender.Send(
                new GetPayoutsQuery(status, page ?? Paging.DefaultPage, pageSize ?? Paging.DefaultPageSize),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();
    }
}
