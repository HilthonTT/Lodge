namespace Lodge.Contracts.Payments;

/// <summary>
/// The host's payout account (<c>GET /hosting/payouts/account</c>).
/// </summary>
/// <param name="Status"><c>"NotConnected"</c>, <c>"Pending"</c> or <c>"Active"</c>.</param>
/// <param name="PayoutsEnabled">Whether payouts can be sent to the account.</param>
/// <param name="Mode"><c>"Stripe"</c> or <c>"Simulated"</c>.</param>
public sealed record PayoutAccountResponse(string Status, bool PayoutsEnabled, string Mode);
