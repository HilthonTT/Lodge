namespace Lodge.Contracts.Messaging;

/// <summary>
/// The conversation the inquiry was posted to (new or existing) and the posted message.
/// </summary>
public sealed record SentMessageResponse(Guid ConversationId, MessageResponse Message);
