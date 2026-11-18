namespace Lodge.Contracts.Notifications;

/// <summary>
/// The unread counters pushed over SignalR (<c>unreadCounts</c>).
/// </summary>
/// <param name="Messages">Unread messages across all conversations.</param>
/// <param name="Notifications">Unread in-app notifications.</param>
public sealed record UnreadCountsResponse(int Messages, int Notifications);
