using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences.CreateReview;

internal sealed class CreateExperienceReviewCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IExperienceRepository experienceRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateExperienceReviewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateExperienceReviewCommand request, CancellationToken cancellationToken)
    {
        ExperienceBooking? booking = await experienceRepository.GetBookingAsync(request.ExperienceBookingId, cancellationToken);

        if (booking is null || booking.GuestId != userIdentifierProvider.UserId)
        {
            return Result.Failure<Guid>(ExperienceBookingErrors.NotFound(request.ExperienceBookingId));
        }

        if (await experienceRepository.HasReviewAsync(booking.Id, cancellationToken))
        {
            return Result.Failure<Guid>(ExperienceBookingErrors.AlreadyReviewed);
        }

        Result<ExperienceReview> review = ExperienceReview.Create(
            booking,
            request.Rating,
            request.Comment,
            dateTimeProvider.UtcNow);

        if (review.IsFailure)
        {
            return Result.Failure<Guid>(review.Error);
        }

        experienceRepository.InsertReview(review.Value);

        return review.Value.Id;
    }
}
