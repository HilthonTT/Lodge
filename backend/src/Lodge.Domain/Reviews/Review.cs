using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Abstractions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews.Events;

namespace Lodge.Domain.Reviews;

/// <summary>
/// A guest's review of a listing after a completed stay.
/// </summary>
/// <remarks>
/// Reviews are double-blind: a review is only visible to its author until it is published, which happens when both
/// the guest and the host have reviewed the stay, or when the review window (<see cref="Booking.ReviewWindowDays"/>
/// days after check-out) closes. Published reviews can no longer be edited; the host can reply to them.
/// </remarks>
public sealed class Review : Entity, IAuditableEntity
{
    /// <summary>
    /// The maximum length of a host reply.
    /// </summary>
    public const int ReplyMaxLength = 1000;

    private Review(
        Guid id,
        Guid apartmentId,
        Guid bookingId,
        Guid userId,
        Rating rating,
        CategoryRatings categories,
        Comment comment)
        : base(id)
    {
        ApartmentId = apartmentId;
        BookingId = bookingId;
        UserId = userId;
        Rating = rating;
        Categories = categories;
        Comment = comment;
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private Review()
    {
    }

    public Guid ApartmentId { get; private set; }

    public Guid BookingId { get; private set; }

    public Guid UserId { get; private set; }

    public Rating Rating { get; private set; } = null!;

    public CategoryRatings Categories { get; private set; } = null!;

    public Comment Comment { get; private set; } = null!;

    public bool IsPublished { get; private set; }

    public DateTime? PublishedOnUtc { get; private set; }

    public string? HostReply { get; private set; }

    public DateTime? HostReplyCreatedOnUtc { get; private set; }

    public DateTime? HostReplyModifiedOnUtc { get; private set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    /// <summary>
    /// Creates an unpublished review of a completed stay, within the review window.
    /// </summary>
    public static Result<Review> Create(
        Booking booking,
        Rating rating,
        CategoryRatings categories,
        Comment comment,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(booking);

        Result reviewable = booking.EnsureReviewable(utcNow);

        if (reviewable.IsFailure)
        {
            return Result.Failure<Review>(reviewable.Error == BookingErrors.NotCompleted
                ? ReviewErrors.NotEligible
                : reviewable.Error);
        }

        var review = new Review(
            Guid.NewGuid(),
            booking.ApartmentId,
            booking.Id,
            booking.UserId,
            rating,
            categories,
            comment);

        review.RaiseDomainEvent(new ReviewCreatedDomainEvent(review.Id));

        return review;
    }

    /// <summary>
    /// Creates a review that is published right away (demo data and reviews imported from older versions).
    /// </summary>
    public static Review CreatePublished(
        Booking booking,
        Rating rating,
        CategoryRatings categories,
        Comment comment,
        DateTime publishedOnUtc)
    {
        ArgumentNullException.ThrowIfNull(booking);

        var review = new Review(Guid.NewGuid(), booking.ApartmentId, booking.Id, booking.UserId, rating, categories, comment);
        review.Publish(publishedOnUtc);

        return review;
    }

    /// <summary>
    /// Changes an unpublished review.
    /// </summary>
    public Result Update(Rating rating, CategoryRatings categories, Comment comment)
    {
        if (IsPublished)
        {
            return Result.Failure(ReviewErrors.AlreadyPublished);
        }

        Rating = rating;
        Categories = categories;
        Comment = comment;

        return Result.Success();
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

        RaiseDomainEvent(new ReviewPublishedDomainEvent(Id));
    }

    /// <summary>
    /// Creates or edits the host's public reply to a published review.
    /// </summary>
    public Result Reply(string? text, DateTime utcNow)
    {
        if (!IsPublished)
        {
            return Result.Failure(ReviewErrors.NotPublished);
        }

        string? reply = text?.Trim();

        if (string.IsNullOrEmpty(reply))
        {
            return Result.Failure(ReviewErrors.ReplyEmpty);
        }

        if (reply.Length > ReplyMaxLength)
        {
            return Result.Failure(ReviewErrors.ReplyTooLong);
        }

        if (HostReply is null)
        {
            HostReplyCreatedOnUtc = utcNow;
            HostReplyModifiedOnUtc = null;

            RaiseDomainEvent(new ReviewRepliedDomainEvent(Id));
        }
        else
        {
            HostReplyModifiedOnUtc = utcNow;
        }

        HostReply = reply;

        return Result.Success();
    }

    /// <summary>
    /// Removes the host's reply, if any.
    /// </summary>
    public void RemoveReply()
    {
        HostReply = null;
        HostReplyCreatedOnUtc = null;
        HostReplyModifiedOnUtc = null;
    }
}
