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
