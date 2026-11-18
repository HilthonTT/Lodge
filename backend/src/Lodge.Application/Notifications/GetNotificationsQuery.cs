using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Notifications;

namespace Lodge.Application.Notifications;

/// <summary>
/// Gets one page of the caller's in-app notifications, newest first (<c>GET /notifications</c>).
/// </summary>
public sealed record GetNotificationsQuery(bool UnreadOnly, int Page, int PageSize) : IQuery<PagedList<NotificationResponse>>;
