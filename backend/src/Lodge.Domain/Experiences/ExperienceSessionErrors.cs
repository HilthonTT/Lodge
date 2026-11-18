using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Experiences;

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
