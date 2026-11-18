using FluentValidation;
using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Application.Payments;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;

namespace Lodge.Application.Hosting.Bookings;

/// <summary>
/// The host cancels a confirmed booking before check-in (<c>PUT /hosting/bookings/{id}/cancel</c>): the guest gets a
/// full refund, the payout is cancelled and the cancellation counts in the host's Superhost cancellation rate.
/// </summary>
public sealed record HostCancelBookingCommand(Guid BookingId) : ICommand;

internal sealed class HostCancelBookingCommandHandler(
    IHostAccess hostAccess,
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    BookingPayments bookingPayments,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<HostCancelBookingCommand>
{
    public async Task<Result> Handle(HostCancelBookingCommand request, CancellationToken cancellationToken)
    {
        Result<Booking> booking = await HostBookings.GetManagedBookingAsync(
            hostAccess,
            bookingRepository,
            apartmentRepository,
            request.BookingId,
            cancellationToken);

        if (booking.IsFailure)
        {
            return booking;
        }

        Result<RefundQuote> cancelled = booking.Value.CancelByHost(dateTimeProvider.UtcNow);

        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        await bookingPayments.AdjustPayoutAsync(booking.Value, cancelled.Value, cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// The host reviews the guest of a completed stay (<c>POST /hosting/bookings/{id}/guest-review</c>), within the review
/// window. When the guest has reviewed the listing too, both reviews are published.
/// </summary>
public sealed record CreateGuestReviewCommand(Guid BookingId, int Rating, string Comment) : ICommand<Guid>;

internal sealed class CreateGuestReviewCommandValidator : AbstractValidator<CreateGuestReviewCommand>
{
    public CreateGuestReviewCommandValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithError(ValidationErrors.CreateReview.RatingMustBeBetweenOneAndFive);

        RuleFor(x => x.Comment)
            .NotEmpty().WithError(ValidationErrors.CreateReview.CommentIsRequired)
            .MaximumLength(Comment.MaxLength).WithError(ValidationErrors.CreateReview.CommentMustNotBeAbove(Comment.MaxLength));
    }
}

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
