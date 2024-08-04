using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Users.Update;
using MediatR;

namespace Lodge.Application.Users;

/// <summary>
/// Represents the user cache invalidation handler.
/// </summary>
internal sealed class UserCacheInvalidationHandler(ICacheService cacheService) : INotificationHandler<UserUpdatedEvent>
{
    /// <inheritdoc />
    public Task Handle(UserUpdatedEvent notification, CancellationToken cancellationToken)
    {
        return HandleInternalAsync(notification.UserId, cancellationToken);
    }

    /// <summary>
    /// Handles the cache invalidation.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    private Task HandleInternalAsync(Guid userId, CancellationToken cancellationToken)
    {
        return cacheService.RemoveAsync(CacheKeys.Users.GetById(userId), cancellationToken);
    }
}
