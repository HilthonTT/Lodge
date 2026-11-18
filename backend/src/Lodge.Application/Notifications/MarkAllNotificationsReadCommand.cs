using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Notifications;

/// <summary>
/// Marks all of the caller's notifications as read (<c>PUT /notifications/read-all</c>).
/// </summary>
public sealed record MarkAllNotificationsReadCommand : ICommand;
