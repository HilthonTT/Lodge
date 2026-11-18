using Lodge.Application.Hosting.Calendar;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Hosting;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Endpoints.Apartments;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Hosting;

/// <summary>
/// The host's calendar of a listing: availability, blocks and custom nightly prices.
/// </summary>
internal sealed class Calendar : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("hosting/listings/{apartmentId:guid}/calendar", async (
            Guid apartmentId,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            IDateTimeProvider dateTimeProvider,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            DateOnly start = from ?? DateOnly.FromDateTime(dateTimeProvider.UtcNow);
            DateOnly end = to ?? start.AddDays(GetCalendar.DefaultCalendarDays - 1);

            Result<List<HostCalendarDayResponse>> result = await sender.Send(
                new GetHostCalendarQuery(apartmentId, start, end),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPut("hosting/listings/{apartmentId:guid}/calendar", async (
            Guid apartmentId,
            UpdateCalendarRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(
                new UpdateCalendarCommand(
                    apartmentId,
                    request.From,
                    request.To,
                    request.Blocked,
                    request.Price,
                    request.ClearPrice),
                cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();
    }
}
