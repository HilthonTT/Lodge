using Lodge.Application.Files.GetById;
using Lodge.Contracts.Storage;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Files;

/// <summary>
/// Represents the endpoint for fetching a file by its identifier.
/// </summary>
internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("files/{fileId}", async (
            Guid fileId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new GetFileByIdQuery(fileId);

            Result<FileResponse> result = await sender.Send(query, cancellationToken);

            return result.Match(
                result => Results.File(result.Stream, result.ContentType),
                CustomResults.Problem);
        })
        .WithTags(Tags.Files);
    }
}
