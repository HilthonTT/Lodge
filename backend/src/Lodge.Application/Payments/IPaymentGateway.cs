namespace Lodge.Application.Payments;

/// <summary>
/// Moves money: refunds to guests, transfers to hosts and Stripe Connect onboarding.
/// </summary>
internal interface IPaymentGateway
{
    /// <summary>
    /// Refunds part or all of a payment and returns the refund id.
    /// </summary>
    Task<string> RefundAsync(
        string paymentIntentId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Transfers money to a host's connected account and returns the transfer id.
    /// </summary>
    Task<string> TransferAsync(
        decimal amount,
        string currency,
        string destinationAccountId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a connected (Express) account and returns its id.
    /// </summary>
    Task<string> CreateConnectedAccountAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an onboarding link for a connected account.
    /// </summary>
    Task<string> CreateOnboardingLinkAsync(
        string accountId,
        string refreshUrl,
        string returnUrl,
        CancellationToken cancellationToken);
}
