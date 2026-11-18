using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lodge.Application.Reviews.Publish;

internal sealed partial class PublishReviewsJob(
    IDbContext context,
    IUnitOfWork unitOfWork,
    ICacheService cacheService,
    IDateTimeProvider dateTimeProvider,
    ILogger<PublishReviewsJob> logger) : IPublishReviewsJob
{
    public async Task<int> PublishExpiredAsync(CancellationToken cancellationToken = default)
    {
        DateTime utcNow = dateTimeProvider.UtcNow;

        // The window is open up to and including check-out + ReviewWindowDays.
        DateOnly lastClosedCheckOut = DateOnly.FromDateTime(utcNow).AddDays(-Booking.ReviewWindowDays - 1);

        IQueryable<Guid> closedBookings = context.Set<Booking>()
            .Where(booking => booking.Duration.End <= lastClosedCheckOut)
            .Select(booking => booking.Id);

        List<Review> reviews = await context.Set<Review>()
            .Where(review => !review.IsPublished && closedBookings.Contains(review.BookingId))
            .ToListAsync(cancellationToken);

        List<GuestReview> guestReviews = await context.Set<GuestReview>()
            .Where(review => !review.IsPublished && closedBookings.Contains(review.BookingId))
            .ToListAsync(cancellationToken);

        reviews.ForEach(review => review.Publish(utcNow));
        guestReviews.ForEach(review => review.Publish(utcNow));

        int published = reviews.Count + guestReviews.Count;

        if (published > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (Guid apartmentId in reviews.Select(review => review.ApartmentId).Distinct())
            {
                await cacheService.RemoveAsync(CacheKeys.Apartments.GetById(apartmentId), cancellationToken);
            }
        }

        LogPublished(logger, published);

        return published;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {Count} reviews whose review window closed")]
    private static partial void LogPublished(ILogger logger, int count);
}
