using Lodge.Application.Users.Update;
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
/// Represents the endpoint for updating a user.
/// </summary>
internal sealed class Update : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users/{userId}", async (
            Guid userId, 
            UpdateUserRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateUserCommand(
                userId, 
                request.FirstName, 
                request.LastName, 
                request.ImageId);

            Result result = await sender.Send(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Users);
    }
}
