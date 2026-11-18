using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Messaging;

namespace Lodge.Application.Messaging;

/// <summary>
/// Gets one page of the caller's conversations, newest message first (<c>GET /conversations</c>).
/// </summary>
public sealed record GetConversationsQuery(int Page, int PageSize) : IQuery<PagedList<ConversationResponse>>;
