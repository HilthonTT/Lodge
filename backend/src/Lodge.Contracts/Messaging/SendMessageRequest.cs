namespace Lodge.Contracts.Messaging;

/// <summary>
/// A new message in a conversation (<c>POST /conversations/{id}/messages</c>).
/// </summary>
public sealed record SendMessageRequest(string Body);
