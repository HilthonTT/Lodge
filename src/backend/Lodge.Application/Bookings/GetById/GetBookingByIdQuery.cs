using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Bookings;

namespace Lodge.Application.Bookings.GetById;

/// <summary>
/// Represents the query for getting a booking by its identifier.
/// </summary>
/// <param name="BookingId">The booking identifier.</param>
public sealed record GetBookingByIdQuery(Guid BookingId) : IQuery<BookingResponse>;
