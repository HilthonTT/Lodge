using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Reviews;

namespace Lodge.Application.Hosting.Reviews;

/// <summary>
/// Gets the published reviews of the host's listings, newest first, replied or not (<c>GET /hosting/reviews</c>).
/// </summary>
public sealed record GetHostReviewsQuery(Guid? ListingId, int Page, int PageSize) : IQuery<PagedList<ReviewResponse>>;
