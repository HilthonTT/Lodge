using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;
using Lodge.Domain.Users;

namespace Lodge.Application.Reviews.Remove;

/// <summary>
/// Represents the <see cref="RemoveReviewCommand"/> handler.
/// </summary>
/// <param name="userIdentifierProvider">The user identifier provider.</param>
/// <param name="reviewRepository">The user repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class RemoveReviewCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveReviewCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RemoveReviewCommand request, CancellationToken cancellationToken)
    {
        Review? review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
        {
            return Result.Failure(ReviewErrors.NotFound(request.ReviewId));
        }

        if (review.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure(UserErrors.InvalidPermissions);
        }

        reviewRepository.Remove(review);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
