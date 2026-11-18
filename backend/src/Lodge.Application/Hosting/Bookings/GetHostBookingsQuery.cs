using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;

namespace Lodge.Application.Hosting.Bookings;

/// <summary>
/// Gets one page of the bookings of the current user's listings, newest first.
/// </summary>
/// <param name="Status">An optional status filter (1 … 7).</param>
/// <param name="ListingId">An optional listing filter.</param>
/// <param name="Page">The page.</param>
/// <param name="PageSize">The page size.</param>
public sealed record GetHostBookingsQuery(int? Status, Guid? ListingId, int Page, int PageSize)
    : IQuery<PagedList<BookingResponse>>;
