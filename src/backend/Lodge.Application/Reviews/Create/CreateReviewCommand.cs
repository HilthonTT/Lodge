using Lodge.Application.Abstractions.Idempotency;

namespace Lodge.Application.Reviews.Create;

/// <summary>
/// Represents the create review command.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="BookingId">The booking identifier.</param>
/// <param name="Rating">The rating out of 5.</param>
/// <param name="Comment">The comment.</param>
public sealed record CreateReviewCommand(
    Guid RequestId,
    Guid UserId,
    Guid BookingId, 
    int Rating, 
    string Comment) : IdempotentCommand<Guid>(RequestId);
