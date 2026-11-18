using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Reviews;

namespace Lodge.Application.Reviews.GetByApartmentId;

/// <summary>
/// Represents the query for fetching the reviews of the apartment.
/// </summary>
/// <param name="ApartmentId">The apartment identifier.</param>
/// <param name="Page">The current page.</param>
/// <param name="PageSize">The page size.</param>
public sealed record GetReviewsByApartmentIdQuery(
    Guid ApartmentId,
    int Page,
    int PageSize) : IQuery<PagedList<ReviewResponse>>;
