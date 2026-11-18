using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Realtime;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Application.Notifications;

internal sealed class MarkNotificationReadCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbContext context,
    IRealtimeOutbox realtimeOutbox,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        Notification? notification = await context.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == userId && n.InApp, cancellationToken);

        if (notification is null)
        {
            return Result.Failure(NotificationErrors.NotFound(request.NotificationId));
        }

        notification.MarkRead(dateTimeProvider.UtcNow);
        realtimeOutbox.CountsChanged(userId);

        return Result.Success();
    }
}
