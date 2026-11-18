using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Payments;

namespace Lodge.Application.Payouts;

/// <summary>
/// Gets the caller's payout account (<c>GET /hosting/payouts/account</c>).
/// </summary>
public sealed record GetPayoutAccountQuery : IQuery<PayoutAccountResponse>;
