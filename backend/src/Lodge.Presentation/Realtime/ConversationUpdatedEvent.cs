namespace Lodge.Presentation.Realtime;

/// <summary>
/// The payload of <c>conversationUpdated</c>.
/// </summary>
public sealed record ConversationUpdatedEvent(Guid ConversationId);
