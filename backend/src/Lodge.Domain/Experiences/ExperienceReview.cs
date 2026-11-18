using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;

namespace Lodge.Domain.Experiences;

/// <summary>
/// A guest's review of a completed experience (one per booking). Unlike stay reviews, it is published right away.
/// </summary>
public sealed class ExperienceReview : Entity
{
    private ExperienceReview(Guid id, ExperienceBooking booking, int rating, string comment, DateTime utcNow)
        : base(id)
    {
        ExperienceId = booking.ExperienceId;
        ExperienceBookingId = booking.Id;
        GuestId = booking.GuestId;
        Rating = rating;
        Comment = comment;
        CreatedOnUtc = utcNow;
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private ExperienceReview()
    {
    }

    public Guid ExperienceId { get; private set; }

    public Guid ExperienceBookingId { get; private set; }

    public Guid GuestId { get; private set; }

    public int Rating { get; private set; }

    public string Comment { get; private set; } = string.Empty;

    public DateTime CreatedOnUtc { get; private set; }

    public static Result<ExperienceReview> Create(ExperienceBooking booking, int rating, string? comment, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(booking);

        if (booking.Status != ExperienceBookingStatus.Completed)
        {
            return Result.Failure<ExperienceReview>(ExperienceBookingErrors.NotCompleted);
        }

        Result<Rating> validRating = Reviews.Rating.Create(rating);
        Result<Comment> validComment = Reviews.Comment.Create(comment?.Trim());

        Result failure = Result.FirstFailureOrSuccess(validRating, validComment);

        if (failure.IsFailure)
        {
            return Result.Failure<ExperienceReview>(failure.Error);
        }

        return new ExperienceReview(Guid.NewGuid(), booking, validRating.Value.Value, validComment.Value.Value, utcNow);
    }
}
