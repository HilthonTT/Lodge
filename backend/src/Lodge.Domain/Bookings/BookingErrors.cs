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
        "The selected dates are not available");

    public static readonly Error NotReserved = Error.Problem(
        "Booking.NotReserved",
        "The booking is not reserved");

    public static readonly Error NotConfirmed = Error.Problem(
        "Booking.NotConfirmed",
        "The booking is not confirmed");

    public static readonly Error NotCancellable = Error.Problem(
        "Booking.NotCancellable",
        "Only pending, reserved or confirmed bookings can be cancelled");

    public static readonly Error AlreadyStarted = Error.Problem(
        "Booking.AlreadyStarted",
        "The booking has already started");

    public static readonly Error NotEnded = Error.Problem(
        "Booking.NotEnded",
        "The booking cannot be completed before the stay has ended");

    public static readonly Error StartDateInPast = Error.Problem(
        "Booking.StartDateInPast",
        "The booking cannot start in the past");

    public static readonly Error NotPendingApproval = Error.Problem(
        "Booking.NotPendingApproval",
        "Only booking requests that are waiting for the host's approval can be accepted or declined");

    public static readonly Error ChangeRequiresCancellation = Error.Conflict(
        "Booking.ChangeRequiresCancellation",
        "The dates of a confirmed booking cannot be changed. Cancel it and book again instead");

    public static readonly Error NotModifiable = Error.Problem(
        "Booking.NotModifiable",
        "Only pending or reserved bookings can be changed");

    public static Error StayTooShort(int minNights) => Error.Problem(
        "Booking.StayTooShort",
        $"This listing requires a stay of at least {minNights} night(s)");

    public static Error StayTooLong(int maxNights) => Error.Problem(
        "Booking.StayTooLong",
        $"This listing allows a stay of at most {maxNights} night(s)");

    public static Error InvalidGuestCount(int maximumGuestCount) => Error.Problem(
        "Booking.InvalidGuestCount",
        $"The number of guests must be between 1 and {maximumGuestCount}");

    public static readonly Error OwnListing = Error.Problem(
        "Booking.OwnListing",
        "Hosts cannot book their own listing");

    public static readonly Error MessageTooLong = Error.Validation(
        "Booking.MessageTooLong",
        $"The message to the host must not be longer than {Booking.MessageToHostMaxLength} characters");
}
