using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Represents the booking's domain errors.
/// </summary>
public static class BookingErrors
{
    public static Error NotFound(Guid bookingId) => Error.NotFound(
        "Booking.Found",
        $"The booking with Id = '{bookingId}' was not found");

    public static readonly Error Overlap = Error.Conflict(
        "Booking.Overlap",
        "The current booking is overlapping with an existing one");

    public static readonly Error NotReserved = Error.Problem(
        "Booking.NotReserved",
        "The booking is not pending");

    public static readonly Error NotConfirmed = Error.Problem(
        "Booking.Confirmed",
        "The booking is not confirmed");

    public static readonly Error AlreadyStarted = Error.Problem(
        "Booking.AlreadyStarted",
        "The booking has already started");
}