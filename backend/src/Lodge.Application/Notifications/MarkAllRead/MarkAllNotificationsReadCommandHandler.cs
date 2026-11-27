using System.Data;
using FluentValidation;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Realtime;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Application.Notifications.MarkAllRead;

internal sealed class MarkAllNotificationsReadCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbContext context,
    IRealtimeOutbox realtimeOutbox,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<MarkAllNotificationsReadCommand>
{
    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;
        DateTime utcNow = dateTimeProvider.UtcNow;

        await context.Set<Notification>()
            .Where(n => n.UserId == userId && n.ReadOnUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.ReadOnUtc, utcNow), cancellationToken);

        realtimeOutbox.CountsChanged(userId);

        return Result.Success();
    }
}
