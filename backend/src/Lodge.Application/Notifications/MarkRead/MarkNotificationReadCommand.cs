using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Notifications.MarkRead;

/// <summary>
/// Marks one of the caller's notifications as read (<c>PUT /notifications/{id}/read</c>); other users' notifications
/// are "not found".
/// </summary>
public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand;
