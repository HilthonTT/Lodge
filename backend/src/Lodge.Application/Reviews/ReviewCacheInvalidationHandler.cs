using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Reviews.Create;
using Lodge.Application.Reviews.Update;
using MediatR;

namespace Lodge.Application.Reviews;

/// <summary>
/// Represents the review cache invalidation handler.
/// </summary>
/// <param name="cacheService">The cache service.</param>
internal sealed class ReviewCacheInvalidationHandler(ICacheService cacheService) :
    INotificationHandler<ReviewCreatedEvent>,
    INotificationHandler<ReviewUpdatedEvent>
{
    /// <inheritdoc />
    public Task Handle(ReviewCreatedEvent notification, CancellationToken cancellationToken)
    {
        return HandleInternalAsync(notification.ReviewId, cancellationToken);
    }

    /// <inheritdoc />
    public Task Handle(ReviewUpdatedEvent notification, CancellationToken cancellationToken)
    {
        return HandleInternalAsync(notification.ReviewId, cancellationToken);
    }

    /// <summary>
    /// Handles the cache invalidation.
    /// </summary>
    /// <param name="reviewId">The review identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    private Task HandleInternalAsync(Guid reviewId, CancellationToken cancellationToken)
    {
        return cacheService.RemoveAsync(CacheKeys.Reviews.GetById(reviewId), cancellationToken);
    }
}
