using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Reviews;

/// <summary>
/// Represents the comment's domain errors
/// </summary>
public static class CommentErrors
{
    public static readonly Error Empty = Error.Validation(
        "Comment.Empty",
        "The comment cannot be empty");

    public static readonly Error TooLong = Error.Validation(
        "Comment.TooLong",
        "The comment exceeds the 500 characters limit");
}
