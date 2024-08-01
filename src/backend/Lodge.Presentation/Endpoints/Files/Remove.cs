using Lodge.Application.Files.Remove;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Files;

/// <summary>
/// Represents the endpoint for removing a file.
/// </summary>
internal sealed class Remove : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("files/{fileId}/users/{userId}", async (
            Guid fileId,
            Guid userId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new RemoveFileCommand(fileId, userId);

            Result result = await sender.Send(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Files);
    }
}
