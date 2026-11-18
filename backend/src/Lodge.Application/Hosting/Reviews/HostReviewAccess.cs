using System.Data;
using FluentValidation;
using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Application.Hosting.Reviews;

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
