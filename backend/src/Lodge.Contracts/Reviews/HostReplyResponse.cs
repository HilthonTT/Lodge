namespace Lodge.Contracts.Reviews;

/// <summary>
/// The host's reply to a review.
/// </summary>
public sealed record HostReplyResponse(string Text, DateTime CreatedOnUtc, DateTime? ModifiedOnUtc);
