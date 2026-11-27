using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Payments;

namespace Lodge.Application.Payments.GetConfig;

/// <summary>
/// Gets the payment mode (<c>GET /payments/config</c>).
/// </summary>
public sealed record GetPaymentsConfigQuery : IQuery<PaymentsConfigResponse>;
