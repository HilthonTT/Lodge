using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;

namespace Lodge.Application.Hosting.Reviews;

internal sealed class DeleteReviewReplyCommandHandler(
    IHostAccess hostAccess,
    IReviewRepository reviewRepository,
    IDbContext context) : ICommandHandler<DeleteReviewReplyCommand>
{
    public async Task<Result> Handle(DeleteReviewReplyCommand request, CancellationToken cancellationToken)
    {
        Result<Review> review = await HostReviewAccess.GetManagedReviewAsync(
            hostAccess,
            reviewRepository,
            context,
            request.ReviewId,
            cancellationToken);

        if (review.IsFailure)
        {
            return review;
        }

        review.Value.RemoveReply();

        return Result.Success();
    }
}
