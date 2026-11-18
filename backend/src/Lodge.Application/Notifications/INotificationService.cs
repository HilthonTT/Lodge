namespace Lodge.Application.Notifications;

/// <summary>
/// Creates in-app notifications (once per source event) and pushes them to the user's browsers.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates the notification unless one with the same dedupe key exists. Notifications of a category the user
    /// turned off in-app are recorded but hidden.
    /// </summary>
    /// <returns><see langword="true"/> when the notification is new (callers send their e-mail only then).</returns>
    Task<bool> NotifyAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}
