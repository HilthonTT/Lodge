using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Reviews;

/// <summary>
/// Represents the rating value object.
/// </summary>
public sealed record Rating
{
    private Rating(int value) => Value = value;

    public int Value { get; }

    /// <summary>
    /// Creates a new rating value object with the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The newly created ratint result.</returns>
    public static Result<Rating> Create(int value)
    {
        if (value is < 1 or > 5)
        {
            return Result.Failure<Rating>(RatingErrors.Invalid);
        }

        return new Rating(value);
    }
}
