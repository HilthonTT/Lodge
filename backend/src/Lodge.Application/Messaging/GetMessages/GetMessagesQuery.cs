using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Messaging;

namespace Lodge.Application.Messaging.GetMessages;

/// <summary>
/// Gets a page of messages of one of the caller's conversations (<c>GET /conversations/{id}/messages</c>): the newest
/// <paramref name="Limit"/> messages older than <paramref name="Before"/> (a message id or an ISO 8601 date and time;
/// none = the latest), in chronological order.
/// </summary>
public sealed record GetMessagesQuery(Guid ConversationId, string? Before, int Limit) : IQuery<MessagePageResponse>;
