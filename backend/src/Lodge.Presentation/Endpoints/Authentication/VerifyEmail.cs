using Lodge.Application.Authentication.VerifyEmail;
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
/// Verifies an e-mail address with the token from the verification e-mail. Anonymous.
/// </summary>
internal sealed class VerifyEmail : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("authentication/verify-email", async (
            VerifyEmailRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new VerifyEmailCommand(request.Token), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Authentication);
    }
}
