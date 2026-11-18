using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;

namespace Lodge.Application.Hosting.Reviews;

internal sealed class ReplyToReviewCommandHandler(
    IHostAccess hostAccess,
    IReviewRepository reviewRepository,
    IDbContext context,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ReplyToReviewCommand>
{
    public async Task<Result> Handle(ReplyToReviewCommand request, CancellationToken cancellationToken)
    {
        Result<Review> review = await HostReviewAccess.GetManagedReviewAsync(
            hostAccess,
            reviewRepository,
            context,
            request.ReviewId,
            cancellationToken);

        return review.IsFailure ? review : review.Value.Reply(request.Text, dateTimeProvider.UtcNow);
    }
}
