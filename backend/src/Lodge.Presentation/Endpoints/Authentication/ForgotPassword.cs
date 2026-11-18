using Lodge.Application.Authentication.ForgotPassword;
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
/// Requests a password reset e-mail. Always 202, whether or not the account exists.
/// </summary>
internal sealed class ForgotPassword : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("authentication/forgot-password", async (
            ForgotPasswordRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);

            return result.Match(() => Results.Accepted(), CustomResults.Problem);
        })
        .WithTags(Tags.Authentication);
    }
}
