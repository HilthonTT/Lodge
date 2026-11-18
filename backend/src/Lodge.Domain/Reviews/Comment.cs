using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Reviews;

/// <summary>
/// Represents the comment value object.
/// </summary>
public sealed record Comment
{
    /// <summary>
    /// The comment maximum length.
    /// </summary>
    public const int MaxLength = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="Comment"/> record.
    /// </summary>
    /// <param name="value">The comment value.</param>
    private Comment(string value) => Value = value;

    /// <summary>
    /// Gets the comment value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="Comment"/> instance based on the specified value.
    /// </summary>
    /// <param name="value">The comment value.</param>
    /// <returns>
    /// The result of the comment creation process containing the comment or an error.
    /// </returns>
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
