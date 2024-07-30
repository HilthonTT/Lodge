using Lodge.Application.Users.ChangePassword;
using Lodge.Contracts.Users;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Users;

/// <summary>
/// Represents the endpoint for changing a user's password.
/// </summary>
internal sealed class ChangePassword : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users/{userId}/change-password", async (
            Guid userId,
            ChangePasswordRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new ChangePasswordCommand(userId, request.Password);

            Result result = await sender.Send(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Users);
    }
}
