using Lodge.Application.Authentication.LoginTwoFactor;
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
/// Completes a two-factor login with a TOTP code or a recovery code.
/// </summary>
internal sealed class LoginTwoFactor : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("authentication/login/two-factor", async (
            LoginTwoFactorRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginTwoFactorCommand(request.TwoFactorToken, request.Code, request.RecoveryCode);

            Result<TokenResponse> result = await sender.Send(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Authentication);
    }
}
