using Lodge.Application.Reviews.Create;
using Lodge.Contracts.Reviews;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Reviews;

/// <summary>
/// Represents the endpoint for creating a review.
/// </summary>
internal sealed class Create : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("reviews", async (
            [FromHeader(Name = "X-Idempotency-Key")] Guid requestId,
            CreateReviewRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateReviewCommand(
                requestId, 
                request.UserId, 
                request.BookingId, 
                request.Rating, 
                request.Comment);

            Result<Guid> result = await sender.Send(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Reviews)
        .RequireAuthorization();
    }
}
