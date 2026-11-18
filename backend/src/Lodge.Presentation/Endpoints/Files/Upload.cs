using Lodge.Application.Files.Upload;
using Lodge.Contracts.Storage;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Files;

/// <summary>
/// Represents the endpoint for uploading a file.
/// </summary>
internal sealed class Upload : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("files", async (
            [FromHeader(Name = IdempotencyKey.HeaderName)] string? idempotencyKey,
            [FromForm] UploadFileRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (!IdempotencyKey.TryParse(idempotencyKey, out Guid requestId))
            {
                return IdempotencyKey.Invalid();
            }

            using Stream stream = request.File.OpenReadStream();

            var command = new UploadFileCommand(
                requestId,
                request.UserId,
                stream,
                request.File.Length,
                request.File.ContentType);

            Result<Guid> result = await sender.Send(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Files)
        .RequireAuthorization()
        // JWT-authenticated API: no cookies, so antiforgery tokens are not needed for form posts.
        .DisableAntiforgery();
    }
}
