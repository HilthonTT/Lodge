using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Abstractions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews.Events;

namespace Lodge.Domain.Reviews;

/// <summary>
/// A host's review of a guest after a completed stay (one per booking). Double-blind like <see cref="Review"/>.
/// </summary>
public sealed class GuestReview : Entity, IAuditableEntity
{
    private GuestReview(Guid id, Booking booking, Guid hostId, Rating rating, Comment comment)
        : base(id)
    {
        BookingId = booking.Id;
        ApartmentId = booking.ApartmentId;
        GuestId = booking.UserId;
        HostId = hostId;
        Rating = rating;
        Comment = comment;
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private GuestReview()
    {
    }

    public Guid BookingId { get; private set; }

    public Guid ApartmentId { get; private set; }

    /// <summary>
    /// Gets the reviewed guest.
    /// </summary>
    public Guid GuestId { get; private set; }

    /// <summary>
    /// Gets the host who wrote the review.
    /// </summary>
    public Guid HostId { get; private set; }

    public Rating Rating { get; private set; } = null!;

    public Comment Comment { get; private set; } = null!;

    public bool IsPublished { get; private set; }

    public DateTime? PublishedOnUtc { get; private set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    /// <summary>
    /// Creates an unpublished review of the guest of a completed stay, within the review window.
    /// </summary>
    public static Result<GuestReview> Create(Booking booking, Guid hostId, Rating rating, Comment comment, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(booking);

        Result reviewable = booking.EnsureReviewable(utcNow);

        if (reviewable.IsFailure)
        {
            return Result.Failure<GuestReview>(reviewable.Error == BookingErrors.NotCompleted
                ? ReviewErrors.NotEligible
                : reviewable.Error);
        }

        return new GuestReview(Guid.NewGuid(), booking, hostId, rating, comment);
    }

    /// <summary>
    /// Creates a review that is published right away (demo data).
    /// </summary>
    public static GuestReview CreatePublished(Booking booking, Guid hostId, Rating rating, Comment comment, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(booking);

        var review = new GuestReview(Guid.NewGuid(), booking, hostId, rating, comment);
        review.Publish(utcNow);

        return review;
    }

    /// <summary>
    /// Publishes the review. Does nothing when it is already published.
    /// </summary>
    public void Publish(DateTime utcNow)
    {
        if (IsPublished)
        {
            return;
        }

        IsPublished = true;
        PublishedOnUtc = utcNow;

        RaiseDomainEvent(new GuestReviewPublishedDomainEvent(Id));
    }
}
