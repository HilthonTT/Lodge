using System.Security.Claims;
using Lodge.Application.Experiences;
using Lodge.Contracts.Common;
using Lodge.Contracts.Experiences;
using Lodge.Contracts.Payments;
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

/// <summary>
/// The guest's experience bookings.
/// </summary>
internal sealed class ExperienceBookings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("experience-bookings", async (
            [FromHeader(Name = IdempotencyKey.HeaderName)] string? idempotencyKey,
            ReserveExperienceRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (!IdempotencyKey.TryParse(idempotencyKey, out Guid requestId))
            {
                return IdempotencyKey.Invalid();
            }

            Result<Guid> result = await sender.Send(
                new ReserveExperienceCommand(requestId, request.SessionId, request.GuestCount),
                cancellationToken);

            return result.Match(
                id => Results.Created($"/api/v1/experience-bookings/{id}", new CreatedResponse(id)),
                CustomResults.Problem);
        })
        .WithTags(Tags.Experiences)
        .RequireAuthorization();

        app.MapPost("experience-bookings/{experienceBookingId:guid}/checkout", async (
            Guid experienceBookingId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<CheckoutResponse> result = await sender.Send(
                new ExperienceCheckoutCommand(experienceBookingId),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Experiences)
        .RequireAuthorization();

        app.MapGet("experience-bookings/{experienceBookingId:guid}", async (
            Guid experienceBookingId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<ExperienceBookingResponse> result = await sender.Send(
                new GetExperienceBookingQuery(experienceBookingId),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Experiences)
        .RequireAuthorization();

        app.MapPut("experience-bookings/{experienceBookingId:guid}/cancel", async (
            Guid experienceBookingId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new CancelExperienceBookingCommand(experienceBookingId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Experiences)
        .RequireAuthorization();

        app.MapPost("experience-bookings/{experienceBookingId:guid}/review", async (
            Guid experienceBookingId,
            ExperienceReviewRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await sender.Send(
                new CreateExperienceReviewCommand(experienceBookingId, request.Rating, request.Comment),
                cancellationToken);

            return result.Match(id => Results.Created($"/api/v1/experience-bookings/{experienceBookingId}/review", new CreatedResponse(id)), CustomResults.Problem);
        })
        .WithTags(Tags.Experiences)
        .RequireAuthorization();

        app.MapGet("users/{userId:guid}/experience-bookings", async (
            Guid userId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<List<ExperienceBookingResponse>> result = await sender.Send(
                new GetUserExperienceBookingsQuery(userId),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Experiences)
        .RequireAuthorization();

        app.MapPost("payments/simulated/experience-bookings/{experienceBookingId:guid}/complete", async (
            Guid experienceBookingId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(
                new CompleteSimulatedExperiencePaymentCommand(experienceBookingId),
                cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Payments)
        .RequireAuthorization();
    }
}

/// <summary>
/// The host's experience management. Every endpoint requires a verified e-mail address (403
/// <c>User.EmailNotVerified</c>); experiences of other hosts give 403 <c>Experience.NotHost</c>.
/// </summary>
internal sealed class HostingExperiences : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("hosting/experiences", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<PagedList<HostExperienceSummaryResponse>> result = await sender.Send(
                new GetHostExperiencesQuery(page ?? Paging.DefaultPage, pageSize ?? Paging.DefaultPageSize),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapGet("hosting/experiences/bookings", async (
            [FromQuery] Guid? experienceId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<PagedList<ExperienceBookingResponse>> result = await sender.Send(
                new GetHostExperienceBookingsQuery(experienceId, page ?? Paging.DefaultPage, pageSize ?? Paging.DefaultPageSize),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapGet("hosting/experiences/{experienceId:guid}", async (
            Guid experienceId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<ExperienceResponse> result = await sender.Send(new GetHostExperienceQuery(experienceId), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPost("hosting/experiences", async (
            ExperienceRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await sender.Send(new CreateExperienceCommand(request), cancellationToken);

            return result.Match(
                id => Results.Created($"/api/v1/hosting/experiences/{id}", new CreatedResponse(id)),
                CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPut("hosting/experiences/{experienceId:guid}", async (
            Guid experienceId,
            ExperienceRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new UpdateExperienceCommand(experienceId, request), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPut("hosting/experiences/{experienceId:guid}/status", async (
            Guid experienceId,
            UpdateExperienceStatusRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new SetExperienceStatusCommand(experienceId, request.IsListed), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapDelete("hosting/experiences/{experienceId:guid}", async (
            Guid experienceId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new DeleteExperienceCommand(experienceId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapGet("hosting/experiences/{experienceId:guid}/sessions", async (
            Guid experienceId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<List<HostExperienceSessionResponse>> result = await sender.Send(
                new GetHostExperienceSessionsQuery(experienceId, from, to),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPost("hosting/experiences/{experienceId:guid}/sessions", async (
            Guid experienceId,
            CreateSessionsRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<CreatedSessionsResponse> result = await sender.Send(
                new CreateExperienceSessionsCommand(experienceId, request),
                cancellationToken);

            return result.Match(
                created => Results.Created($"/api/v1/hosting/experiences/{experienceId}/sessions", created),
                CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapDelete("hosting/experiences/{experienceId:guid}/sessions/{sessionId:guid}", async (
            Guid experienceId,
            Guid sessionId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new DeleteExperienceSessionCommand(experienceId, sessionId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();
    }
}
