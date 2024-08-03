using Lodge.Application.Reviews.Remove;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Reviews;

/// <summary>
/// Represents the endpoint for removing the endpoint.
/// </summary>
internal sealed class Remove : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("reviews/{reviewId}", async (
            Guid reviewId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new RemoveReviewCommand(reviewId);

            Result result = await sender.Send(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Reviews)
        .RequireAuthorization();
    }
}
