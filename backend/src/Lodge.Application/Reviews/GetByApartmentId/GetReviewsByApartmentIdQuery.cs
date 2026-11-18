using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Reviews;

namespace Lodge.Application.Reviews.GetByApartmentId;

/// <summary>
/// Gets the published reviews of a listing, plus the unpublished review of the caller (if signed in).
/// </summary>
/// <param name="ApartmentId">The apartment identifier.</param>
/// <param name="Page">The current page.</param>
/// <param name="PageSize">The page size.</param>
/// <param name="CallerId">The signed-in user, or null.</param>
public sealed record GetReviewsByApartmentIdQuery(
    Guid ApartmentId,
    int Page,
    int PageSize,
    Guid? CallerId = null) : IQuery<PagedList<ReviewResponse>>;
