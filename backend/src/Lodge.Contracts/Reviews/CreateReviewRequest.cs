namespace Lodge.Contracts.Reviews;

/// <summary>
/// Represents the create review request.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="BookingId">The booking identifier.</param>
/// <param name="Rating">The overall rating out of 5.</param>
/// <param name="Comment">The comment.</param>
/// <param name="Categories">The required category ratings (each 1 to 5).</param>
public sealed record CreateReviewRequest(
    Guid UserId,
    Guid BookingId,
    int Rating,
    string Comment,
    CategoryRatingsDto? Categories);
