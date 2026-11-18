using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;
using Lodge.Domain.Users;

namespace Lodge.Application.Reviews.Update;

/// <summary>
/// Represents the <see cref="UpdateReviewCommand"/> handler.
/// </summary>
/// <param name="userIdentifierProvider">The user identifier provider.</param>
/// <param name="reviewRepository">The review repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="cacheInvalidator">The cache invalidator.</param>
internal sealed class UpdateReviewCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    ICacheInvalidator cacheInvalidator) : ICommandHandler<UpdateReviewCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure(UserErrors.InvalidPermissions);
        }

        Review? review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
        {
            return Result.Failure(ReviewErrors.NotFound(request.ReviewId));
        }

        if (review.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure(UserErrors.InvalidPermissions);
        }

        Result<Comment> commentResult = Comment.Create(request.Comment);
        Result<Rating> ratingResult = Rating.Create(request.Rating);
        Result<CategoryRatings> categoriesResult = ReviewMapping.ToCategories(request.Categories);

        Result firstFailureOrSuccess = Result.FirstFailureOrSuccess(commentResult, ratingResult, categoriesResult);
        if (firstFailureOrSuccess.IsFailure)
        {
            return Result.Failure(firstFailureOrSuccess.Error);
        }

        Result updated = review.Update(ratingResult.Value, categoriesResult.Value, commentResult.Value);
        if (updated.IsFailure)
        {
            return updated;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // The apartment's average rating and review count are part of the cached apartment.
        cacheInvalidator.InvalidateOnCommit(CacheKeys.Apartments.GetById(review.ApartmentId));

        return Result.Success();
    }
}
