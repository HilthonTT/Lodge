using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Abstractions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews.Events;

namespace Lodge.Domain.Reviews;

/// <summary>
/// Represents the review entity.
/// </summary>
public sealed class Review : Entity, IAuditableEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Review"/> class.
    /// </summary>
    /// <param name="id">The review's id.</param>
    /// <param name="apartmentId">The apartment's id.</param>
    /// <param name="bookingId">The booking's id.</param>
    /// <param name="userId">The user's id.</param>
    /// <param name="rating">The rating's id.</param>
    /// <param name="comment">The comment's id.</param>
    private Review(
        Guid id,
        Guid apartmentId, 
        Guid bookingId, 
        Guid userId, 
        Rating rating, 
        Comment comment)
        : base(id)
    {
        ApartmentId = apartmentId;
        BookingId = bookingId;
        UserId = userId;
        Rating = rating;
        Comment = comment;
    }

    /// <summary>
    /// Initializes a new blank instance of <see cref="Review"/> class
    /// </summary>
    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private Review()
    {
    }

    public Guid ApartmentId { get; private set; }

    public Guid BookingId { get; private set; }

    public Guid UserId { get; private set; }

    public Rating Rating { get; private set; }

    public Comment Comment { get; private set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }


    /// <summary>
    /// Creates a new review.
    /// </summary>
    /// <param name="booking">The booking being reviewed.</param>
    /// <param name="rating">The rating.</param>
    /// <param name="comment">The comment.</param>
    /// <returns>The newly created result of a review instance.</returns>
    public static Result<Review> Create(
        Booking booking,
        Rating rating,
        Comment comment)
    {
        if (booking.Status != BookingStatus.Completed)
        {
            return Result.Failure<Review>(ReviewErrors.NotEligible);
        }

        var review = new Review(
            Guid.NewGuid(),
            booking.ApartmentId,
            booking.Id,
            booking.UserId,
            rating,
            comment);

        review.RaiseDomainEvent(new ReviewCreatedDomainEvent(review.Id));

        return review;
    }
}
