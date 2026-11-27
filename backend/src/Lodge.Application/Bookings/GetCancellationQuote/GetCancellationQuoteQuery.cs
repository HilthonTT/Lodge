using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Bookings;

namespace Lodge.Application.Bookings.GetCancellationQuote;

/// <summary>
/// Quotes the refund the guest would get by cancelling today (<c>GET /bookings/{id}/cancellation-quote</c>).
/// </summary>
public sealed record GetCancellationQuoteQuery(Guid BookingId) : IQuery<CancellationQuoteResponse>;
