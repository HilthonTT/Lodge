using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Hosting.Reviews;

/// <summary>
/// Removes the host's reply to a review (<c>DELETE /hosting/reviews/{reviewId}/reply</c>).
/// </summary>
public sealed record DeleteReviewReplyCommand(Guid ReviewId) : ICommand;
