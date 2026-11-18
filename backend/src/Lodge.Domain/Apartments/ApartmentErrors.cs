using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Apartments;

/// <summary>
/// Represents the apartment's domain errors.
/// </summary>
public static class ApartmentErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Apartment.NotFound",
        $"The apartment with the Id = '{id}' was not found");

    public static readonly Error NotOwner = Error.Permission(
        "Apartment.NotOwner",
        "Only the host of this listing can do that.");

    public static readonly Error HasUpcomingBookings = Error.Conflict(
        "Apartment.HasUpcomingBookings",
        "The listing has upcoming bookings or booking requests. Cancel or decline them before deleting the listing.");

    public static readonly Error InvalidImageCount = Error.Validation(
        "Apartment.InvalidImageCount",
        $"A listing needs between 1 and {Apartment.MaxImages} photos.");

    public static readonly Error DuplicateImage = Error.Validation(
        "Apartment.DuplicateImage",
        "The same photo cannot be added twice.");

    public static Error InvalidImage(Guid imageId) => Error.Validation(
        "Apartment.InvalidImage",
        $"The photo '{imageId}' does not exist or was not uploaded by you.");

    public static readonly Error InvalidPhoto = Error.Validation(
        "Apartment.InvalidPhoto",
        "The photo must be a JPEG, PNG or WebP image.");

    public static readonly Error PhotoTooLarge = Error.Validation(
        "Apartment.PhotoTooLarge",
        "The photo must not be larger than 10 MB.");

    public static readonly Error InvalidStayLimits = Error.Validation(
        "Apartment.InvalidStayLimits",
        $"The minimum and maximum number of nights must be between 1 and {Apartment.MaxNightsLimit}, minimum first.");

    public static readonly Error InvalidDiscount = Error.Validation(
        "Apartment.InvalidDiscount",
        $"Discounts must be between 0 and {Apartment.MaxDiscountPercent} percent.");

    public static readonly Error InvalidCoordinates = Error.Validation(
        "Apartment.InvalidCoordinates",
        "The latitude must be between -90 and 90 and the longitude between -180 and 180.");

    public static readonly Error InvalidCapacity = Error.Validation(
        "Apartment.InvalidCapacity",
        "Bedrooms, beds, bathrooms (in steps of 0.5) and the guest count must be within their limits.");

    public static readonly Error InvalidPrice = Error.Validation(
        "Apartment.InvalidPrice",
        "The nightly price must be positive, the cleaning fee must not be negative and the weekend price, when set, must be positive.");

    public static readonly Error HouseRulesTooLong = Error.Validation(
        "Apartment.HouseRulesTooLong",
        $"The house rules must not be longer than {Apartment.TextMaxLength} characters.");

    public static readonly Error CheckInInstructionsTooLong = Error.Validation(
        "Apartment.CheckInInstructionsTooLong",
        $"The check-in instructions must not be longer than {Apartment.TextMaxLength} characters.");

    public static readonly Error InvalidCalendarRange = Error.Validation(
        "Apartment.InvalidCalendarRange",
        $"The calendar range must start on or before its end and span at most {Apartment.MaxCalendarDays} days.");

    public static readonly Error CalendarInPast = Error.Validation(
        "Apartment.CalendarInPast",
        "Past dates cannot be changed.");
}
