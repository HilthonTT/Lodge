using Lodge.Application.Apartments.GetCalendar;
using Lodge.Application.Apartments.GetPriceRange;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Apartments;

/// <summary>
/// The public calendar of a listing (availability and nightly prices) and the price range of all listings.
/// </summary>
internal sealed class GetCalendar : IEndpoint
{
    /// <summary>
    /// The number of days returned when <c>to</c> is omitted.
    /// </summary>
    internal const int DefaultCalendarDays = 90;

    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("apartments/{apartmentId:guid}/calendar", async (
            Guid apartmentId,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            IDateTimeProvider dateTimeProvider,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            DateOnly start = from ?? DateOnly.FromDateTime(dateTimeProvider.UtcNow);
            DateOnly end = to ?? start.AddDays(DefaultCalendarDays - 1);

            Result<List<ApartmentCalendarDayResponse>> result = await sender.Send(
                new GetApartmentCalendarQuery(apartmentId, start, end),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Apartments);

        app.MapGet("apartments/price-range", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<PriceRangeResponse> result = await sender.Send(new GetPriceRangeQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Apartments);
    }
}
