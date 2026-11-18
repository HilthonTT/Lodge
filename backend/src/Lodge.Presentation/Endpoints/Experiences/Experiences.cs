using System.Security.Claims;
using Lodge.Application.Experiences;
using Lodge.Contracts.Common;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Experiences;

/// <summary>
/// Public experience pages: search, details, sessions and reviews.
/// </summary>
internal sealed class Experiences : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("experiences", async (
            [FromQuery] string? q,
            [FromQuery] string? city,
            [FromQuery] string? category,
            [FromQuery] DateOnly? date,
            [FromQuery] int? guests,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<PagedList<ExperienceSummaryResponse>> result = await sender.Send(
                new SearchExperiencesQuery(
                    q,
                    city,
                    category,
                    date,
                    guests,
                    minPrice,
                    maxPrice,
                    page ?? Paging.DefaultPage,
                    pageSize ?? Paging.DefaultPageSize),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Experiences)
        .AllowAnonymous();

        app.MapGet("experiences/{experienceId:guid}", async (
            Guid experienceId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<ExperienceResponse> result = await sender.Send(
                new GetExperienceByIdQuery(experienceId, user.GetUserIdOrNull()),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Experiences)
        .AllowAnonymous();

        app.MapGet("experiences/{experienceId:guid}/sessions", async (
            Guid experienceId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<List<ExperienceSessionResponse>> result = await sender.Send(
                new GetExperienceSessionsQuery(experienceId, from, to),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Experiences)
        .AllowAnonymous();

        app.MapGet("experiences/{experienceId:guid}/reviews", async (
            Guid experienceId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<PagedList<ExperienceReviewResponse>> result = await sender.Send(
                new GetExperienceReviewsQuery(experienceId, page ?? Paging.DefaultPage, pageSize ?? Paging.DefaultPageSize),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Experiences)
        .AllowAnonymous();
    }
}
