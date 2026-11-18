using Lodge.Application.Authentication.ResendVerification;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Authentication;

/// <summary>
/// Sends a new verification e-mail to the signed-in user. Throttled to once per minute (429).
/// </summary>
internal sealed class ResendVerification : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("authentication/resend-verification", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new ResendVerificationEmailCommand(), cancellationToken);

            return result.Match(() => Results.Accepted(), CustomResults.Problem);
        })
        .WithTags(Tags.Authentication)
        .RequireAuthorization();
    }
}
