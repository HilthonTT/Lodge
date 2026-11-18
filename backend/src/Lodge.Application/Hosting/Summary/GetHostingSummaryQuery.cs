using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Hosting;

namespace Lodge.Application.Hosting.Summary;

/// <summary>
/// Gets the hosting dashboard numbers of the current user.
/// </summary>
/// <remarks>
/// Upcoming stays: reserved or confirmed bookings that start today or later. Earnings come from the payouts (stay
/// subtotal minus the host fee): this month = scheduled, on hold and paid payouts dated this calendar month (UTC),
/// paid total = paid payouts, upcoming payouts = scheduled and on hold payouts. Ratings (published reviews) and the
/// Superhost progress cover every listing of the host, deleted ones included.
/// </remarks>
public sealed record GetHostingSummaryQuery : IQuery<HostingSummaryResponse>;
