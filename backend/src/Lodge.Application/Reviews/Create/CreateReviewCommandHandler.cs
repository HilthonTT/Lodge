using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;
using Lodge.Domain.Users;

namespace Lodge.Application.Reviews.Create;

/// <summary>
/// The guest reviews the listing of a completed stay, within the review window. The review stays unpublished (only
/// visible to its author) until the host has reviewed the guest too, or until the window closes.
/// </summary>
internal sealed class CreateReviewCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IReviewRepository reviewRepository,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    ICacheInvalidator cacheInvalidator,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateReviewCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<Guid>(UserErrors.InvalidPermissions);
        }

        Booking? booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<Guid>(BookingErrors.NotFound(request.BookingId));
        }

        if (booking.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<Guid>(UserErrors.InvalidPermissions);
        }

        if (await reviewRepository.GetByBookingIdAsync(booking.Id, cancellationToken) is not null)
        {
            return Result.Failure<Guid>(ReviewErrors.AlreadyReviewed);
        }

        Result<Rating> ratingResult = Rating.Create(request.Rating);
        Result<Comment> commentResult = Comment.Create(request.Comment);
        Result<CategoryRatings> categoriesResult = ReviewMapping.ToCategories(request.Categories);

        Result firstFailureOrSuccess = Result.FirstFailureOrSuccess(ratingResult, commentResult, categoriesResult);
        if (firstFailureOrSuccess.IsFailure)
        {
            return Result.Failure<Guid>(firstFailureOrSuccess.Error);
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        Result<Review> reviewResult = Review.Create(
            booking,
            ratingResult.Value,
            categoriesResult.Value,
            commentResult.Value,
            utcNow);

        if (reviewResult.IsFailure)
        {
            return Result.Failure<Guid>(reviewResult.Error);
        }

        Review review = reviewResult.Value;

        reviewRepository.Insert(review);

        // Double-blind: when the host has already reviewed the guest, both reviews are published.
        GuestReview? guestReview = await reviewRepository.GetGuestReviewByBookingIdAsync(booking.Id, cancellationToken);

        if (guestReview is not null)
        {
            review.Publish(utcNow);
            guestReview.Publish(utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // The apartment's average rating and review count are part of the cached apartment.
        cacheInvalidator.InvalidateOnCommit(CacheKeys.Apartments.GetById(booking.ApartmentId));

        return review.Id;
    }
}
