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
}
