using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Experiences;

/// <summary>
/// Contains the experience booking errors.
/// </summary>
public static class ExperienceBookingErrors
{
    public static Error NotFound(Guid bookingId) => Error.NotFound(
        "ExperienceBooking.NotFound",
        $"The experience booking with the Id = '{bookingId}' was not found.");

    public static readonly Error OwnExperience = Error.Problem(
        "ExperienceBooking.OwnExperience",
        "You cannot book your own experience.");

    public static readonly Error InvalidGuestCount = Error.Validation(
        "ExperienceBooking.InvalidGuestCount",
        "The number of guests must be at least 1 and at most the experience's maximum.");

    public static readonly Error NotReserved = Error.Problem(
        "ExperienceBooking.NotReserved",
        "Only reserved (unpaid) experience bookings can be paid.");

    public static readonly Error NotCancellable = Error.Problem(
        "ExperienceBooking.NotCancellable",
        "Only reserved or confirmed experience bookings can be cancelled before the session starts.");

    public static readonly Error NotCompleted = Error.Permission(
        "ExperienceBooking.NotCompleted",
        "Only completed experiences can be reviewed.");

    public static readonly Error AlreadyReviewed = Error.Problem(
        "ExperienceBooking.AlreadyReviewed",
        "This experience booking has already been reviewed.");
}
