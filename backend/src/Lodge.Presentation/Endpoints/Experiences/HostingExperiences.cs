using Lodge.Application.Experiences;
using Lodge.Application.Experiences.Create;
using Lodge.Application.Experiences.CreateSessions;
using Lodge.Application.Experiences.Delete;
using Lodge.Application.Experiences.DeleteSession;
using Lodge.Application.Experiences.GetHostBookings;
using Lodge.Application.Experiences.GetHostExperience;
using Lodge.Application.Experiences.GetHostExperiences;
using Lodge.Application.Experiences.GetHostSessions;
using Lodge.Application.Experiences.SetStatus;
using Lodge.Application.Experiences.Update;
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
