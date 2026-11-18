using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Hosting;

namespace Lodge.Application.Hosting.Listings;

/// <summary>
/// Gets one page of the current user's (not deleted) listings, newest first.
/// </summary>
public sealed record GetHostListingsQuery(int Page, int PageSize) : IQuery<PagedList<HostListingSummaryResponse>>;
