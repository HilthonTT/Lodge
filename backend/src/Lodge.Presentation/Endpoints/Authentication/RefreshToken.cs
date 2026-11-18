using Lodge.Application.Authentication.RefreshToken;
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
/// Issues a fresh access token for the signed-in user (e.g. to update the <c>emailVerified</c> claim).
/// </summary>
internal sealed class RefreshToken : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("authentication/refresh-token", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<TokenResponse> result = await sender.Send(new RefreshTokenCommand(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Authentication)
        .RequireAuthorization();
    }
}
