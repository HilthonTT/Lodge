namespace Lodge.Contracts.Reviews;

/// <summary>
/// Represents the create request.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="BookingId">The booking identifier.</param>
/// <param name="Rating">The rating out of 5.</param>
/// <param name="Comment">The comment.</param>
public sealed record CreateReviewRequest(Guid UserId, Guid BookingId, int Rating, string Comment);
