namespace Lodge.Domain.Reviews;

/// <summary>
/// Represents the repository of reviews (guest to listing) and guest reviews (host to guest).
/// </summary>
public interface IReviewRepository
{
    /// <summary>
    /// Gets the review with the specified identifier.
    /// </summary>
    Task<Review?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the guest's review of the stay, if any.
    /// </summary>
    Task<Review?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the host's review of the guest of the stay, if any.
    /// </summary>
    Task<GuestReview?> GetGuestReviewByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    void Insert(Review review);

    void Insert(GuestReview review);

    void Remove(Review review);
}
