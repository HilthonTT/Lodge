using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Experiences;

/// <summary>
/// Contains the experience errors.
/// </summary>
public static class ExperienceErrors
{
    public static Error NotFound(Guid experienceId) => Error.NotFound(
        "Experience.NotFound",
        $"The experience with the Id = '{experienceId}' was not found.");

    public static readonly Error NotHost = Error.Permission(
        "Experience.NotHost",
        "Only the host of the experience can manage it.");

    public static readonly Error InvalidTitle = Error.Validation(
        "Experience.InvalidTitle",
        $"The title is required and must not be longer than {Experience.TitleMaxLength} characters.");

    public static readonly Error InvalidDescription = Error.Validation(
        "Experience.InvalidDescription",
        $"The description is required and must not be longer than {Experience.DescriptionMaxLength} characters.");

    public static readonly Error InvalidCategory = Error.Validation(
        "Experience.InvalidCategory",
        "The category must be Food, Outdoors, Art, Culture, Sports, Wellness, Nightlife or Workshops.");

    public static readonly Error InvalidLocation = Error.Validation(
        "Experience.InvalidLocation",
        "The city and country are required (at most 100 characters each), the latitude must be between -90 and 90 and the longitude between -180 and 180.");

    public static readonly Error InvalidMeetingPoint = Error.Validation(
        "Experience.InvalidMeetingPoint",
        $"The meeting point is required and must not be longer than {Experience.MeetingPointMaxLength} characters.");

    public static readonly Error InvalidDuration = Error.Validation(
        "Experience.InvalidDuration",
        $"The duration must be between {Experience.MinDurationMinutes} and {Experience.MaxDurationMinutes} minutes.");

    public static readonly Error InvalidPrice = Error.Validation(
        "Experience.InvalidPrice",
        "The price per guest must be greater than 0 and at most 100000.");

    public static readonly Error InvalidCurrency = Error.Validation(
        "Experience.InvalidCurrency",
        "The currency must be USD or EUR.");

    public static readonly Error InvalidMaxGuests = Error.Validation(
        "Experience.InvalidMaxGuests",
        $"The maximum number of guests must be between 1 and {Experience.MaxGuestsLimit}.");

    public static readonly Error InvalidLanguages = Error.Validation(
        "Experience.InvalidLanguages",
        $"Give 1 to {Experience.MaxLanguages} languages as ISO 639-1 codes (e.g. \"en\", \"fr\").");

    public static readonly Error InvalidIncluded = Error.Validation(
        "Experience.InvalidIncluded",
        $"Give at most {Experience.MaxIncludedItems} included items of at most {Experience.IncludedItemMaxLength} characters each.");

    public static readonly Error TooManyImages = Error.Validation(
        "Experience.TooManyImages",
        $"An experience can have at most {Experience.MaxImages} photos.");

    public static Error InvalidImage(Guid imageId) => Error.Validation(
        "Experience.InvalidImage",
        $"The photo '{imageId}' was not uploaded by you.");

    public static readonly Error HasUpcomingBookings = Error.Conflict(
        "Experience.HasUpcomingBookings",
        "The experience has upcoming bookings: cancel its sessions first.");
}

/// <summary>
/// Contains the experience session errors.
/// </summary>
public static class ExperienceSessionErrors
{
    public static Error NotFound(Guid sessionId) => Error.NotFound(
        "ExperienceSession.NotFound",
        $"The session with the Id = '{sessionId}' was not found.");

    public static readonly Error InvalidCapacity = Error.Validation(
        "ExperienceSession.InvalidCapacity",
        "The capacity must be between 1 and the experience's maximum number of guests.");

    public static readonly Error StartsInPast = Error.Validation(
        "ExperienceSession.StartsInPast",
        "The session must start in the future.");

    public static readonly Error InvalidSchedule = Error.Validation(
        "ExperienceSession.InvalidSchedule",
        "Give either 'startsAtUtc' or a 'recurring' schedule.");

    public static readonly Error InvalidRecurrence = Error.Validation(
        "ExperienceSession.InvalidRecurrence",
        "A recurring schedule needs days of week (e.g. \"Monday\"), a start time (HH:mm), an IANA time zone and 'from' on or before 'until'.");

    public static readonly Error InvalidTimeZone = Error.Validation(
        "ExperienceSession.InvalidTimeZone",
        "Unknown time zone: use an IANA time zone id such as \"Europe/Amsterdam\".");

    public static Error TooManySessions(int max) => Error.Validation(
        "ExperienceSession.TooManySessions",
        $"At most {max} sessions can be created at once.");

    public static readonly Error NoSessions = Error.Validation(
        "ExperienceSession.NoSessions",
        "The schedule does not produce any future session.");

    public static readonly Error Cancelled = Error.Problem(
        "ExperienceSession.Cancelled",
        "The session was cancelled.");

    public static readonly Error AlreadyStarted = Error.Problem(
        "ExperienceSession.AlreadyStarted",
        "The session has already started.");

    public static readonly Error NotEnoughSpots = Error.Conflict(
        "ExperienceSession.NotEnoughSpots",
        "There are not enough spots left in this session.");
}

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
