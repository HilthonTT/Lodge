using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;
using Lodge.Domain.Users;
using MediatR;

namespace Lodge.Application.Reviews.Create;

/// <summary>
/// Represents the <see cref="CreateReviewCommand"/> command.
/// </summary>
/// <param name="userIdentifierProvider">The user identifier.</param>
/// <param name="reviewRepository">The review repository.</param>
/// <param name="bookingRepository">The booking repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="publisher">The publisher.</param>
internal sealed class CreateReviewCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IReviewRepository reviewRepository,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    IPublisher publisher) : ICommandHandler<CreateReviewCommand, Guid>
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

        if (await reviewRepository.HasAlreadyReviewed(booking.ApartmentId, userIdentifierProvider.UserId, cancellationToken))
        {
            return Result.Failure<Guid>(ReviewErrors.AlreadyReviewed);
        }

        Result<Rating> ratingResult = Rating.Create(request.Rating);
        Result<Comment> commentResult = Comment.Create(request.Comment);

        Result firstFailureOrSuccess = Result.FirstFailureOrSuccess(ratingResult, commentResult);
        if (firstFailureOrSuccess.IsFailure)
        {
            return Result.Failure<Guid>(firstFailureOrSuccess.Error);
        }

        Result<Review> reviewResult = Review.Create(booking, ratingResult.Value, commentResult.Value);
        if (reviewResult.IsFailure)
        {
            return Result.Failure<Guid>(reviewResult.Error);
        }

        Review review = reviewResult.Value;

        reviewRepository.Insert(review);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new ReviewCreatedEvent(review.Id), cancellationToken);

        return review.Id;
    }
}
