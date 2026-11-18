using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using System.Data;

namespace Lodge.Application.Bookings.GetByUserId;

/// <summary>
/// Represents the <see cref="GetBookingsByUserIdQuery"/> handler.
/// </summary>
/// <param name="factory">The database connection factory.</param>
/// <param name="userIdentifierProvider">The user identifier provider.</param>
internal sealed class GetBookingsByUserIdQueryHandler(
    IDbConnectionFactory factory, 
    IUserIdentifierProvider userIdentifierProvider)
    : IQueryHandler<GetBookingsByUserIdQuery, List<BookingResponse>>
{
    /// <inheritdoc />
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

        return bookings;
    }
}
