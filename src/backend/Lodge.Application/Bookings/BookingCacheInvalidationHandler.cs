﻿using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Bookings.Cancel;
using Lodge.Application.Bookings.Complete;
using Lodge.Application.Bookings.Confirm;
using Lodge.Application.Bookings.Reject;
using Lodge.Application.Bookings.Reserve;
using MediatR;

namespace Lodge.Application.Bookings;

/// <summary>
/// Represents the booking cache invalidation handler.
/// </summary>
/// <param name="cacheService">The cache service.</param>
internal sealed class BookingCacheInvalidationHandler(ICacheService cacheService) :
    INotificationHandler<BookingCancelledEvent>,
    INotificationHandler<BookingCompletedEvent>,
    INotificationHandler<BookingConfirmedEvent>,
    INotificationHandler<BookingRejectedEvent>,
    INotificationHandler<BookingReservedEvent>
{
    /// <inheritdoc />
    public Task Handle(BookingCancelledEvent notification, CancellationToken cancellationToken)
    {
        return HandleInternalAsync(notification.BookingId, notification.UserId, cancellationToken);
    }

    /// <inheritdoc />
    public Task Handle(BookingCompletedEvent notification, CancellationToken cancellationToken)
    {
        return HandleInternalAsync(notification.BookingId, notification.UserId, cancellationToken);
    }

    /// <inheritdoc />
    public Task Handle(BookingConfirmedEvent notification, CancellationToken cancellationToken)
    {
        return HandleInternalAsync(notification.BookingId, notification.UserId, cancellationToken);
    }

    /// <inheritdoc />
    public Task Handle(BookingRejectedEvent notification, CancellationToken cancellationToken)
    {
        return HandleInternalAsync(notification.BookingId, notification.UserId, cancellationToken);
    }

    /// <inheritdoc />
    public Task Handle(BookingReservedEvent notification, CancellationToken cancellationToken)
    {
        return HandleInternalAsync(notification.BookingId, notification.UserId, cancellationToken);
    }

    /// <summary>
    /// Handles the cache invalidation.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    private Task HandleInternalAsync(Guid bookingId, Guid userId, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>
        {
            cacheService.RemoveAsync(CacheKeys.Bookings.GetById(bookingId), cancellationToken),
            cacheService.RemoveAsync(CacheKeys.Bookings.GetByUserId(userId), cancellationToken)
        };

        return Task.WhenAll(tasks);
    }
}
