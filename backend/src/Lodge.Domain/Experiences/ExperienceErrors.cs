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
