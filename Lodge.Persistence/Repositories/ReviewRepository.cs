using Lodge.Domain.Core.Primitives.Maybe;
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
    public async Task<Maybe<Review?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Reviews.FirstOrDefaultAsync(review =>  review.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> HasAlreadyReviewed(Guid apartmentId, Guid userId, CancellationToken cancellationToken = default)
    {
        return context.Reviews.AnyAsync(
            review => review.ApartmentId == apartmentId &&
            review.UserId == userId,
            cancellationToken);
    }

    /// <inheritdoc />
    public void Insert(Review review)
    {
        context.Reviews.Add(review);
    }

    /// <inheritdoc />
    public void Remove(Review review)
    {
        context.Reviews.Remove(review);
    }
}
