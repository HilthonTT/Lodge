using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Messaging;

namespace Lodge.Application.Messaging;

/// <summary>
/// Counts the caller's unread messages across conversations (<c>GET /conversations/unread-count</c>).
/// </summary>
public sealed record GetUnreadMessageCountQuery : IQuery<UnreadCountResponse>;
