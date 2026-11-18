namespace Lodge.Contracts.Payments;

/// <summary>
/// The payment configuration the frontend needs (<c>GET /payments/config</c>).
/// </summary>
/// <param name="Mode"><c>"Stripe"</c> or <c>"Simulated"</c>.</param>
public sealed record PaymentsConfigResponse(string Mode);
