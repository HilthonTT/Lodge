namespace Lodge.Contracts.Reviews;

/// <summary>
/// Represents the update review request.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="Rating">The rating.</param>
/// <param name="Comment">The comment.</param>
public sealed record UpdateReviewRequest(Guid UserId, int Rating, string Comment);
