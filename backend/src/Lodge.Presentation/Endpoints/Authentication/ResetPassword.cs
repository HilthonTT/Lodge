using Lodge.Application.Authentication.ResetPassword;
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
/// Sets a new password with the token from the password reset e-mail.
/// </summary>
internal sealed class ResetPassword : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("authentication/reset-password", async (
            ResetPasswordRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new ResetPasswordCommand(request.Token, request.Password), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Authentication);
    }
}
