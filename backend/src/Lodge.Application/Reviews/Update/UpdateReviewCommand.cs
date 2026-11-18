using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Reviews.Update;

/// <summary>
/// Represents the update review command.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="ReviewId">The review identifier.</param>
/// <param name="Rating">The rating.</param>
/// <param name="Comment">The comment.</param>
public sealed record UpdateReviewCommand(
    Guid UserId, 
    Guid ReviewId, 
    int Rating, 
    string Comment) : ICommand;
