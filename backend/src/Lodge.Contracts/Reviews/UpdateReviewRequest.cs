namespace Lodge.Contracts.Reviews;

/// <summary>
/// Represents the update review request (only unpublished reviews can be changed).
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="Rating">The overall rating.</param>
/// <param name="Comment">The comment.</param>
/// <param name="Categories">The required category ratings (each 1 to 5).</param>
public sealed record UpdateReviewRequest(Guid UserId, int Rating, string Comment, CategoryRatingsDto? Categories);
