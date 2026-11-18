using Lodge.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the review repository.
/// </summary>
/// <param name="context">The database context.</param>
internal sealed class ReviewRepository(LodgeDbContext context) : IReviewRepository
{
    /// <inheritdoc />
    public Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Reviews.FirstOrDefaultAsync(review => review.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Review?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        context.Reviews.FirstOrDefaultAsync(review => review.BookingId == bookingId, cancellationToken);

    /// <inheritdoc />
    public Task<GuestReview?> GetGuestReviewByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        context.GuestReviews.FirstOrDefaultAsync(review => review.BookingId == bookingId, cancellationToken);

    /// <inheritdoc />
    public void Insert(Review review) => context.Reviews.Add(review);

    /// <inheritdoc />
    public void Insert(GuestReview review) => context.GuestReviews.Add(review);

    /// <inheritdoc />
    public void Remove(Review review) => context.Reviews.Remove(review);
}
