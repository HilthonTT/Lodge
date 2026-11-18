using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Notifications;

/// <summary>
/// Contains the notification errors.
/// </summary>
public static class NotificationErrors
{
    /// <summary>
    /// The notification does not exist or belongs to someone else.
    /// </summary>
    public static Error NotFound(Guid notificationId) => Error.NotFound(
        "Notification.NotFound",
        $"The notification with the Id = '{notificationId}' was not found.");
}
