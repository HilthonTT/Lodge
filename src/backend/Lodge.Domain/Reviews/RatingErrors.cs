using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Reviews;

/// <summary>
/// Represents rating's domain errors.
/// </summary>
public static class RatingErrors
{
    public static readonly Error Invalid = Error.Problem(
        "Rating.Invalid", "The rating is invalid");
}
