using Lodge.Domain.Notifications;

namespace Lodge.Application.Notifications;

/// <summary>
/// Reads the users' notification preferences (defaults: everything on).
/// </summary>
public interface INotificationPreferencesProvider
{
    Task<NotificationPreferences> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> AllowsEmailAsync(Guid userId, NotificationCategory category, CancellationToken cancellationToken = default);
}
