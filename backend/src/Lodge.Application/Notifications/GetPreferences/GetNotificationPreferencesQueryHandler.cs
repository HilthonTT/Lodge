using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Notifications;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Notifications.GetPreferences;

internal sealed class GetNotificationPreferencesQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    INotificationPreferencesProvider preferencesProvider)
    : IQueryHandler<GetNotificationPreferencesQuery, NotificationPreferencesDto>
{
    public async Task<Result<NotificationPreferencesDto>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<NotificationPreferencesDto>(UserErrors.InvalidPermissions);
        }

        return NotificationPreferencesMapper.ToDto(await preferencesProvider.GetAsync(request.UserId, cancellationToken));
    }
}
