using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Contains the booking errors.
/// </summary>
public static class BookingErrors
{
    public static Error NotFound(Guid bookingId) => Error.NotFound(
        "Booking.NotFound",
        $"The booking with Id = '{bookingId}' was not found");

    public static readonly Error Overlap = Error.Conflict(
        "Booking.Overlap",
        "The current booking is overlapping with an existing one");

    public static readonly Error NotReserved = Error.Problem(
        "Booking.NotReserved",
        "The booking is not reserved");

    public static readonly Error NotConfirmed = Error.Problem(
        "Booking.NotConfirmed",
        "The booking is not confirmed");

    public static readonly Error NotCancellable = Error.Problem(
        "Booking.NotCancellable",
        "Only reserved or confirmed bookings can be cancelled");

    public static readonly Error AlreadyStarted = Error.Problem(
        "Booking.AlreadyStarted",
        "The booking has already started");

    public static readonly Error NotEnded = Error.Problem(
        "Booking.NotEnded",
        "The booking cannot be completed before the stay has ended");

    public static readonly Error StartDateInPast = Error.Problem(
        "Booking.StartDateInPast",
        "The booking cannot start in the past");
}
