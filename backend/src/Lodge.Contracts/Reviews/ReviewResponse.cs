namespace Lodge.Contracts.Reviews;

/// <summary>
/// Represents the review response.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="ApartmentId">The apartment identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="Rating">The rating.</param>
/// <param name="Comment">The comment.</param>
/// <param name="CreatedOnUtc">The created on date and time in UTC format.</param>
/// <param name="ModifiedOnUtc">The modified on date and time in UTC format.</param>
public sealed record ReviewResponse(
    Guid Id,
    Guid ApartmentId,
    Guid UserId,
    int Rating,
    string Comment,
    DateTime CreatedOnUtc,
    DateTime? ModifiedOnUtc);
