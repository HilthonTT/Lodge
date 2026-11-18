using Lodge.Application.Authentication.External;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Authentication;

/// <summary>
/// Lists the configured social login providers.
/// </summary>
internal sealed class ExternalProviders : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("authentication/providers", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<ExternalProviderResponse>> result =
                await sender.Send(new GetExternalProvidersQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Authentication);
    }
}
