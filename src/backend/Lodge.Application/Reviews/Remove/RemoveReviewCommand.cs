using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Reviews.Remove;

/// <summary>
/// Represents the remove review command.
/// </summary>
/// <param name="ReviewId">The review identifier.</param>
public sealed record RemoveReviewCommand(Guid ReviewId) : ICommand;
