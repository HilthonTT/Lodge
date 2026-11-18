using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Bookings;

namespace Lodge.Application.Bookings.GetByUserId;

/// <summary>
/// Gets the bookings of the current user. Not cached: the result is user-specific and the
/// ownership check must run on every request.
/// </summary>
public sealed record GetBookingsByUserIdQuery(Guid UserId) : IQuery<List<BookingResponse>>;
