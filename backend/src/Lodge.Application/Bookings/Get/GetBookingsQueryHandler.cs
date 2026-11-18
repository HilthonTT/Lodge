using Lodge.Application.Bookings.Expire;
using Microsoft.Extensions.Options;
using System.Data;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Bookings.Get;

internal sealed class GetBookingsQueryHandler(
    IDbConnectionFactory factory,
    IAssetUrlResolver assetUrlResolver,
    IUserIdentifierProvider userIdentifierProvider,
    IDateTimeProvider dateTimeProvider,
    IOptions<ExpireBookingRequestsOptions> expiryOptions)
    : IQueryHandler<GetBookingsQuery, PagedList<BookingResponse>>
{
    public async Task<Result<PagedList<BookingResponse>>> Handle(
        GetBookingsQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<BookingResponse> items, int totalCount) = await BookingQueries.GetPageAsync(
            connection,
            request.Status,
            request.Page,
            request.PageSize);

        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        foreach (BookingResponse booking in items)
        {
            BookingQueries.Present(booking, userIdentifierProvider.UserId, assetUrlResolver, today, expiryOptions.Value.ExpiryHours);
        }

        return new PagedList<BookingResponse>(items, request.Page, request.PageSize, totalCount);
    }
}
