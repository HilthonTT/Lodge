using System.Data;
using FluentValidation;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Apartments;
using Lodge.Application.Core.Extensions;
using Lodge.Application.Reviews;
using Lodge.Contracts.Common;
using Lodge.Contracts.Reviews;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Application.Hosting.Reviews;

/// <summary>
/// Gets the published reviews of the host's listings, newest first, replied or not (<c>GET /hosting/reviews</c>).
/// </summary>
public sealed record GetHostReviewsQuery(Guid? ListingId, int Page, int PageSize) : IQuery<PagedList<ReviewResponse>>;

internal sealed class GetHostReviewsQueryValidator : AbstractValidator<GetHostReviewsQuery>
{
    public GetHostReviewsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}

internal sealed class GetHostReviewsQueryHandler(
    IHostAccess hostAccess,
    IDbConnectionFactory factory,
    ApartmentReader apartmentReader) : IQueryHandler<GetHostReviewsQuery, PagedList<ReviewResponse>>
{
    public async Task<Result<PagedList<ReviewResponse>>> Handle(
        GetHostReviewsQuery request,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<PagedList<ReviewResponse>>(caller.Error);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<ReviewResponse> items, int totalCount) = await ReviewQueries.GetByHostAsync(
            connection,
            caller.Value.UserId,
            request.ListingId,
            request.Page,
            request.PageSize,
            apartmentReader.AvatarUrl);

        return new PagedList<ReviewResponse>(items, request.Page, request.PageSize, totalCount);
    }
}

/// <summary>
/// Creates or edits the host's public reply to a published review of one of their listings
/// (<c>PUT /hosting/reviews/{reviewId}/reply</c>).
/// </summary>
public sealed record ReplyToReviewCommand(Guid ReviewId, string Text) : ICommand;

internal sealed class ReplyToReviewCommandValidator : AbstractValidator<ReplyToReviewCommand>
{
    public ReplyToReviewCommandValidator() =>
        RuleFor(x => x.Text)
            .Must(text => !string.IsNullOrWhiteSpace(text)).WithError(ReviewErrors.ReplyEmpty)
            .Must(text => text is null || text.Trim().Length <= Review.ReplyMaxLength).WithError(ReviewErrors.ReplyTooLong);
}

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

/// <summary>
/// Removes the host's reply to a review (<c>DELETE /hosting/reviews/{reviewId}/reply</c>).
/// </summary>
public sealed record DeleteReviewReplyCommand(Guid ReviewId) : ICommand;

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

internal static class HostReviewAccess
{
    /// <summary>
    /// Loads a review of one of the caller's listings, deleted ones included (admins may manage every review).
    /// </summary>
    public static async Task<Result<Review>> GetManagedReviewAsync(
        IHostAccess hostAccess,
        IReviewRepository reviewRepository,
        IDbContext context,
        Guid reviewId,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<Review>(caller.Error);
        }

        Review? review = await reviewRepository.GetByIdAsync(reviewId, cancellationToken);

        if (review is null)
        {
            return Result.Failure<Review>(ReviewErrors.NotFound(reviewId));
        }

        Guid? ownerId = await context.Set<Apartment>()
            .IgnoreQueryFilters()
            .Where(apartment => apartment.Id == review.ApartmentId)
            .Select(apartment => (Guid?)apartment.OwnerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerId is null)
        {
            return Result.Failure<Review>(ReviewErrors.NotFound(reviewId));
        }

        Result access = caller.Value.EnsureCanManage(ownerId.Value);

        return access.IsFailure ? Result.Failure<Review>(access.Error) : review;
    }
}
