using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Hosting.Reviews;

/// <summary>
/// Creates or edits the host's public reply to a published review of one of their listings
/// (<c>PUT /hosting/reviews/{reviewId}/reply</c>).
/// </summary>
public sealed record ReplyToReviewCommand(Guid ReviewId, string Text) : ICommand;
