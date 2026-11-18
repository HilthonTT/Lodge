using Lodge.Application.Reviews.Update;
using Lodge.Contracts.Reviews;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Reviews;

/// <summary>
/// Represents the 
/// </summary>
internal sealed class Update : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("reviews/{reviewId}", async (
            Guid reviewId,
            UpdateReviewRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateReviewCommand(request.UserId, reviewId, request.Rating, request.Comment);

            Result result = await sender.Send(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Reviews)
        .RequireAuthorization();
    }
}
