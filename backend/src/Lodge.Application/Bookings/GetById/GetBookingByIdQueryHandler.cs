using System.Data;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Bookings;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Bookings.GetById;

internal sealed class GetBookingByIdQueryHandler(
    IDbConnectionFactory factory,
    IUserIdentifierProvider userIdentifierProvider,
    IAssetUrlResolver assetUrlResolver) : IQueryHandler<GetBookingByIdQuery, BookingResponse>
{
    public async Task<Result<BookingResponse>> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        BookingResponse? booking = await BookingQueries.GetByIdAsync(connection, request.BookingId);

        if (booking is null)
        {
            return Result.Failure<BookingResponse>(BookingErrors.NotFound(request.BookingId));
        }

        Guid callerId = userIdentifierProvider.UserId;

        // The guest and the listing's host can see the booking.
        if (booking.UserId != callerId && booking.ListingHostId != callerId)
        {
            return Result.Failure<BookingResponse>(UserErrors.InvalidPermissions);
        }

        return BookingQueries.Present(booking, callerId, assetUrlResolver);
    }
}
