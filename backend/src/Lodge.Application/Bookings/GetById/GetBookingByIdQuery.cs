using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Bookings;

namespace Lodge.Application.Bookings.GetById;

/// <summary>
/// Gets a booking of the current user (as the guest or as the listing's host). Not cached: the result is user-specific and the
/// ownership check must run on every request.
/// </summary>
public sealed record GetBookingByIdQuery(Guid BookingId) : IQuery<BookingResponse>;
