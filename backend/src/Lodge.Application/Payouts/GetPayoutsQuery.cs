using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Payments;

namespace Lodge.Application.Payouts;

/// <summary>
/// Gets one page of the caller's payouts, newest first (<c>GET /hosting/payouts</c>).
/// </summary>
/// <param name="Status">An optional status filter (<c>Scheduled</c>, <c>OnHold</c>, <c>Paid</c>, <c>Failed</c>, <c>Cancelled</c>).</param>
/// <param name="Page">The page.</param>
/// <param name="PageSize">The page size.</param>
public sealed record GetPayoutsQuery(string? Status, int Page, int PageSize) : IQuery<PagedList<PayoutResponse>>;
