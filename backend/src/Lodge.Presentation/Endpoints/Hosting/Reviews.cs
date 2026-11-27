using Lodge.Application.Hosting.Reviews;
using Lodge.Application.Hosting.Reviews.DeleteReply;
using Lodge.Application.Hosting.Reviews.Get;
using Lodge.Application.Hosting.Reviews.Reply;
using Lodge.Contracts.Common;
using Lodge.Contracts.Reviews;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Hosting;

/// <summary>
/// The reviews of the host's listings and the host's replies.
/// </summary>
internal sealed class Reviews : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("hosting/reviews", async (
            [FromQuery] Guid? listingId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<PagedList<ReviewResponse>> result = await sender.Send(
                new GetHostReviewsQuery(listingId, page ?? Paging.DefaultPage, pageSize ?? Paging.DefaultPageSize),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPut("hosting/reviews/{reviewId:guid}/reply", async (
            Guid reviewId,
            ReviewReplyRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new ReplyToReviewCommand(reviewId, request.Text), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapDelete("hosting/reviews/{reviewId:guid}/reply", async (
            Guid reviewId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new DeleteReviewReplyCommand(reviewId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();
    }
}
