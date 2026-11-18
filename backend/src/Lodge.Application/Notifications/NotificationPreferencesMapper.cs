using Lodge.Contracts.Notifications;
using Lodge.Domain.Notifications;

namespace Lodge.Application.Notifications;

internal static class NotificationPreferencesMapper
{
    public static NotificationPreferencesDto ToDto(NotificationPreferences preferences) => new(
        new NotificationChannelDto(
            preferences.EmailMessages,
            preferences.EmailReservations,
            preferences.EmailReviews,
            preferences.EmailPayouts,
            preferences.EmailReminders),
        new NotificationChannelDto(
            preferences.InAppMessages,
            preferences.InAppReservations,
            preferences.InAppReviews,
            preferences.InAppPayouts,
            preferences.InAppReminders));
}
