using Lodge.Application.Authentication.Login;
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
/// Represents the endpoint for logging in a user.
/// </summary>
internal sealed class Login : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("authentication/login", async (
            LoginRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand(request.Email, request.Password);

            Result<TokenResponse> result = await sender.Send(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Authentication);
    }
}
