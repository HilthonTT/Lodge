using Dapper;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Bookings;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using System.Data;

namespace Lodge.Application.Bookings.GetById;

/// <summary>
/// Reresents the <see cref="GetBookingByIdQuery"/> handler.
/// </summary>
/// <param name="factory">The database connection factory.</param>
internal sealed class GetBookingByIdQueryHandler(
    IDbConnectionFactory factory,
    IUserIdentifierProvider userIdentifierProvider) : IQueryHandler<GetBookingByIdQuery, BookingResponse>
{
    /// <inheritdoc />
    public async Task<Result<BookingResponse>> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        BookingResponse? booking = await BookingQueries.GetByIdAsync(connection, request.BookingId);

        if (booking is null)
        {
            return Result.Failure<BookingResponse>(BookingErrors.NotFound(request.BookingId));
        }

        if (booking.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<BookingResponse>(UserErrors.InvalidPermissions);
        }

        return booking;
    }
}
