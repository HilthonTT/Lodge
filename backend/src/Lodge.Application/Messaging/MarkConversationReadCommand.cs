using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Messaging;

/// <summary>
/// Moves the caller's read marker of a conversation to now (<c>PUT /conversations/{id}/read</c>).
/// </summary>
public sealed record MarkConversationReadCommand(Guid ConversationId) : ICommand;
