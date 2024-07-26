using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Reviews;

/// <summary>
/// Represents the comment value object.
/// </summary>
public sealed record Comment
{
    public const int MaxLength = 500;

    private Comment(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Creates a new comment value object with the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The newly created comment result.</returns>
    public static Result<Comment> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Comment>(CommentErrors.Empty);
        }

        if (value.Length > MaxLength)
        {
            return Result.Failure<Comment>(CommentErrors.TooLong);
        }

        return new Comment(value);
    }
}
