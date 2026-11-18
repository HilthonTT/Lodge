using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Messaging;

namespace Lodge.Application.Notifications;

/// <summary>
/// Counts the caller's unread in-app notifications (<c>GET /notifications/unread-count</c>).
/// </summary>
public sealed record GetUnreadNotificationCountQuery : IQuery<UnreadCountResponse>;
