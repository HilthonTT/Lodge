using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;

namespace Lodge.Application.Hosting.Bookings.CreateGuestReview;

internal sealed class CreateGuestReviewCommandHandler(
    IHostAccess hostAccess,
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IReviewRepository reviewRepository,
    ICacheInvalidator cacheInvalidator,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateGuestReviewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGuestReviewCommand request, CancellationToken cancellationToken)
    {
        Result<Booking> booking = await HostBookings.GetManagedBookingAsync(
            hostAccess,
            bookingRepository,
            apartmentRepository,
            request.BookingId,
            cancellationToken);

        if (booking.IsFailure)
        {
            return Result.Failure<Guid>(booking.Error);
        }

        if (await reviewRepository.GetGuestReviewByBookingIdAsync(booking.Value.Id, cancellationToken) is not null)
        {
            return Result.Failure<Guid>(ReviewErrors.AlreadyReviewed);
        }

        Result<Rating> rating = Rating.Create(request.Rating);
        Result<Comment> comment = Comment.Create(request.Comment?.Trim());

        Result firstFailure = Result.FirstFailureOrSuccess(rating, comment);

        if (firstFailure.IsFailure)
        {
            return Result.Failure<Guid>(firstFailure.Error);
        }

        Apartment apartment = (await apartmentRepository.GetByIdAsync(booking.Value.ApartmentId, cancellationToken))!;
        DateTime utcNow = dateTimeProvider.UtcNow;

        Result<GuestReview> review = GuestReview.Create(booking.Value, apartment.OwnerId, rating.Value, comment.Value, utcNow);

        if (review.IsFailure)
        {
            return Result.Failure<Guid>(review.Error);
        }

        reviewRepository.Insert(review.Value);

        // Double-blind: both sides have reviewed the stay, so both reviews are published.
        Review? listingReview = await reviewRepository.GetByBookingIdAsync(booking.Value.Id, cancellationToken);

        if (listingReview is not null)
        {
            listingReview.Publish(utcNow);
            review.Value.Publish(utcNow);
            cacheInvalidator.InvalidateOnCommit(CacheKeys.Apartments.GetById(listingReview.ApartmentId));
        }

        return review.Value.Id;
    }
}
