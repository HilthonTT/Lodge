using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;

namespace Lodge.Application.Bookings.Get;

/// <summary>
/// Gets one page of every user's bookings, newest first. Admin only: authorization is enforced by the endpoint
/// policy. Not cached, so status changes are visible immediately.
/// </summary>
/// <param name="Status">The optional booking status filter (1 Reserved … 7 Expired).</param>
/// <param name="Page">The current page.</param>
/// <param name="PageSize">The page size.</param>
public sealed record GetBookingsQuery(int? Status, int Page, int PageSize) : IQuery<PagedList<BookingResponse>>;
