using Lodge.Domain.Bookings;

namespace Lodge.Application.Reviews.Publish;

/// <summary>
/// Publishes the reviews whose review window has closed.
/// </summary>
public interface IPublishReviewsJob
{
    /// <summary>
    /// Publishes every unpublished review (guest to listing and host to guest) of stays that checked out more than
    /// <see cref="Booking.ReviewWindowDays"/> days ago.
    /// </summary>
    /// <returns>The number of published reviews.</returns>
    Task<int> PublishExpiredAsync(CancellationToken cancellationToken = default);
}
