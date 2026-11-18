namespace Lodge.Application.Payments;

/// <summary>
/// Picks the gateway: the configured mode for new operations; for existing payments, accounts and transfers, the
/// gateway that created them (simulated ids start with <c>sim_</c>), whatever the current mode.
/// </summary>
internal sealed class PaymentGateways(
    IPaymentModeProvider modeProvider,
    StripePaymentGateway stripe,
    SimulatedPaymentGateway simulated)
{
    public PaymentMode Mode => modeProvider.Mode;

    public IPaymentGateway Current => modeProvider.Mode == PaymentMode.Simulated ? simulated : stripe;

    /// <summary>
    /// Gets the gateway that owns a payment, account or transfer id.
    /// </summary>
    public IPaymentGateway For(string? externalId) => SimulatedIds.IsSimulated(externalId) ? simulated : stripe;
}
