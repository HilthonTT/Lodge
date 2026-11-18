using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Reviews;

/// <summary>
/// Represents the reviews' domain errors.
/// </summary>
public static class ReviewErrors
{
    public static Error NotFound(Guid review) => Error.NotFound(
        "Review.NotFound",
        $"The review with the Id = '{review}' was not found");

    public static readonly Error NotEligible = Error.Permission(
        "Review.NotEligible",
        "The review is not eligible because the booking has not yet been completed");

    public static readonly Error AlreadyReviewed = Error.Problem(
        "Review.AlreadyReviewed",
        "You have already reviewed this.");

    public static readonly Error InvalidCategoryRating = Error.Validation(
        "Review.InvalidCategoryRating",
        "Every category rating (cleanliness, accuracy, checkIn, communication, location, value) must be between 1 and 5");

    public static readonly Error AlreadyPublished = Error.Conflict(
        "Review.AlreadyPublished",
        "Published reviews can no longer be changed");

    public static readonly Error NotPublished = Error.Problem(
        "Review.NotPublished",
        "Hosts can only reply to published reviews");

    public static readonly Error ReplyEmpty = Error.Validation(
        "Review.ReplyEmpty",
        "The reply cannot be empty");

    public static readonly Error ReplyTooLong = Error.Validation(
        "Review.ReplyTooLong",
        $"The reply must not be longer than {Review.ReplyMaxLength} characters");
}
