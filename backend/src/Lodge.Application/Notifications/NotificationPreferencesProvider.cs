using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Application.Notifications;

internal sealed class NotificationPreferencesProvider(IDbContext context) : INotificationPreferencesProvider
{
    public async Task<NotificationPreferences> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.Set<NotificationPreferences>()
            .AsNoTracking()
            .FirstOrDefaultAsync(preferences => preferences.Id == userId, cancellationToken)
        ?? NotificationPreferences.CreateDefault(userId);

    public async Task<bool> AllowsEmailAsync(
        Guid userId,
        NotificationCategory category,
        CancellationToken cancellationToken = default) =>
        (await GetAsync(userId, cancellationToken)).AllowsEmail(category);
}
