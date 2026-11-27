using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Notifications;

namespace Lodge.Application.Notifications.GetUnreadCounts;

/// <summary>
/// Gets both unread counters of a user (sent to each new SignalR connection).
/// </summary>
public sealed record GetUnreadCountsQuery(Guid UserId) : IQuery<UnreadCountsResponse>;
