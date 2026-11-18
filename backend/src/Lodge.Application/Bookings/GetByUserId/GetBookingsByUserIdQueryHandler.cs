using System.Data;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Bookings.GetByUserId;

internal sealed class GetBookingsByUserIdQueryHandler(
    IDbConnectionFactory factory,
    IUserIdentifierProvider userIdentifierProvider,
    IAssetUrlResolver assetUrlResolver)
    : IQueryHandler<GetBookingsByUserIdQuery, List<BookingResponse>>
{
    public async Task<Result<List<BookingResponse>>> Handle(
        GetBookingsByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<List<BookingResponse>>(UserErrors.InvalidPermissions);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        List<BookingResponse> bookings = await BookingQueries.GetByUserIdAsync(connection, request.UserId);

        foreach (BookingResponse booking in bookings)
        {
            BookingQueries.Present(booking, request.UserId, assetUrlResolver);
        }

        return bookings;
    }
}
