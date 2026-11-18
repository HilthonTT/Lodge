namespace Lodge.Contracts.Reviews;

/// <summary>
/// Creates or edits the host's reply to a published review (<c>PUT /hosting/reviews/{reviewId}/reply</c>).
/// </summary>
/// <param name="Text">The reply (1 to 1000 characters).</param>
public sealed record ReviewReplyRequest(string Text);
