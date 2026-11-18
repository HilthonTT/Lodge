using System.Data;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Bookings;
using Lodge.Application.Bookings.Expire;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Domain.Core.Primitives;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Hosting.Bookings;

internal sealed class GetHostBookingsQueryHandler(
    IHostAccess hostAccess,
    IDbConnectionFactory factory,
    IAssetUrlResolver assetUrlResolver,
    IDateTimeProvider dateTimeProvider,
    IOptions<ExpireBookingRequestsOptions> expiryOptions) : IQueryHandler<GetHostBookingsQuery, PagedList<BookingResponse>>
{
    public async Task<Result<PagedList<BookingResponse>>> Handle(
        GetHostBookingsQuery request,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<PagedList<BookingResponse>>(caller.Error);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<BookingResponse> items, int totalCount) = await BookingQueries.GetPageAsync(
            connection,
            request.Status,
            request.Page,
            request.PageSize,
            hostId: caller.Value.UserId,
            apartmentId: request.ListingId);

        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        foreach (BookingResponse booking in items)
        {
            BookingQueries.Present(booking, caller.Value.UserId, assetUrlResolver, today, expiryOptions.Value.ExpiryHours);
        }

        return new PagedList<BookingResponse>(items, request.Page, request.PageSize, totalCount);
    }
}
