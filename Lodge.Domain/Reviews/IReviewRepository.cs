using Lodge.Domain.Core.Primitives.Maybe;

namespace Lodge.Domain.Reviews;

/// <summary>
/// Represents the review repository interface
/// </summary>
public interface IReviewRepository
{
    /// <summary>
    /// Gets the review with the specified identifier.
    /// </summary>
    /// <param name="id">The review identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The maybe instance that may contain the review with the specified identifier.</returns>
    Task<Maybe<Review>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has already reviewed a specified apartment.
    /// </summary>
    /// <param name="apartmentId">The apartment identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation that returns true if the user has already reviewed the apartment, otherwise false.</returns>
    Task<bool> HasAlreadyReviewed(Guid apartmentId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts the specified review into the database.
    /// </summary>
    /// <param name="review">The review to be inserted into the database.</param>
    void Insert(Review review);

    /// <summary>
    /// Removes the specified review from the database.
    /// </summary>
    /// <param name="review">The review to be removed from the database.</param>
    void Remove(Review review);
}
