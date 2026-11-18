using Lodge.Application.Hosting.Bookings;
using Lodge.Application.Hosting.Summary;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Contracts.Hosting;
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
/// The bookings of the host's listings, booking request answers and the hosting dashboard summary.
/// </summary>
internal sealed class Bookings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("hosting/bookings", async (
            [FromQuery] int? status,
            [FromQuery] Guid? listingId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<PagedList<BookingResponse>> result = await sender.Send(
                new GetHostBookingsQuery(
                    status,
                    listingId,
                    page ?? Paging.DefaultPage,
                    pageSize ?? Paging.DefaultPageSize),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPut("hosting/bookings/{bookingId:guid}/accept", async (
            Guid bookingId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new AcceptBookingRequestCommand(bookingId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPut("hosting/bookings/{bookingId:guid}/decline", async (
            Guid bookingId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new DeclineBookingRequestCommand(bookingId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPut("hosting/bookings/{bookingId:guid}/cancel", async (
            Guid bookingId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new HostCancelBookingCommand(bookingId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPost("hosting/bookings/{bookingId:guid}/guest-review", async (
            Guid bookingId,
            GuestReviewRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await sender.Send(
                new CreateGuestReviewCommand(bookingId, request.Rating, request.Comment),
                cancellationToken);

            return result.Match(
                id => Results.Created($"hosting/bookings/{bookingId}/guest-review", new CreatedResponse(id)),
                CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapGet("hosting/summary", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<HostingSummaryResponse> result = await sender.Send(new GetHostingSummaryQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();
    }
}
