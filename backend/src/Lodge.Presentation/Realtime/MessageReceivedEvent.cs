using Lodge.Contracts.Messaging;

namespace Lodge.Presentation.Realtime;

/// <summary>
/// The payload of <c>messageReceived</c>.
/// </summary>
public sealed record MessageReceivedEvent(Guid ConversationId, MessageResponse Message);
