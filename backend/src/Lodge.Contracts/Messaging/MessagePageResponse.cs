namespace Lodge.Contracts.Messaging;

/// <summary>
/// One page of messages in chronological order (oldest first). Pass <see cref="NextBefore"/> as <c>before</c> to get
/// the previous (older) page; it is null when there is none.
/// </summary>
public sealed record MessagePageResponse(IReadOnlyList<MessageResponse> Items, bool HasMore, Guid? NextBefore);
