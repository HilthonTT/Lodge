using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Notifications;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Notifications;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Application.Notifications.UpdatePreferences;

internal sealed class UpdateNotificationPreferencesCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbContext context,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateNotificationPreferencesCommand, NotificationPreferencesDto>
{
    public async Task<Result<NotificationPreferencesDto>> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<NotificationPreferencesDto>(UserErrors.InvalidPermissions);
        }

        NotificationPreferences? preferences = await context.Set<NotificationPreferences>()
            .FirstOrDefaultAsync(p => p.Id == request.UserId, cancellationToken);

        if (preferences is null)
        {
            preferences = NotificationPreferences.CreateDefault(request.UserId);
            context.Set<NotificationPreferences>().Add(preferences);
        }

        NotificationChannelDto email = request.Preferences.Email;
        NotificationChannelDto inApp = request.Preferences.InApp;

        preferences.SetEmail(email.Messages, email.Reservations, email.Reviews, email.Payouts, email.Reminders);
        preferences.SetInApp(inApp.Messages, inApp.Reservations, inApp.Reviews, inApp.Payouts, inApp.Reminders);
        preferences.Touch(dateTimeProvider.UtcNow);

        return NotificationPreferencesMapper.ToDto(preferences);
    }
}
