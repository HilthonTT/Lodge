using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Messaging;

namespace Lodge.Application.Messaging.Send;

/// <summary>
/// Sends a message in a conversation the caller takes part in (<c>POST /conversations/{id}/messages</c>).
/// </summary>
public sealed record SendMessageCommand(Guid ConversationId, string Body) : ICommand<MessageResponse>;
