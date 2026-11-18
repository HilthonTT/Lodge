namespace Lodge.Application.Payments;

/// <summary>
/// How payments, refunds and payouts are processed.
/// </summary>
public enum PaymentMode
{
    /// <summary>
    /// Stripe Checkout, Refunds and Connect (requires <c>Stripe:SecretKey</c> and <c>Stripe:WebhookSecret</c>).
    /// </summary>
    Stripe,

    /// <summary>
    /// Everything succeeds immediately without calling Stripe (development and demos).
    /// </summary>
    Simulated,
}
