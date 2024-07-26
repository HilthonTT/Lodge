using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Reviews;

/// <summary>
/// Represents the reviews' domain errors.
/// </summary>
public static class ReviewErrors
{
    public static readonly Error NotEligible = Error.Permission(
        "Review.NotEligible",
        "The review is not eligible because the booking has not yet been completed");
}
