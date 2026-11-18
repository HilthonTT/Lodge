namespace Lodge.Application.Payments;

/// <summary>
/// Succeeds immediately without calling Stripe.
/// </summary>
internal sealed class SimulatedPaymentGateway : IPaymentGateway
{
    public Task<string> RefundAsync(
        string paymentIntentId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken) => Task.FromResult(SimulatedIds.New("re"));

    public Task<string> TransferAsync(
        decimal amount,
        string currency,
        string destinationAccountId,
        string idempotencyKey,
        CancellationToken cancellationToken) => Task.FromResult(SimulatedIds.New("tr"));

    public Task<string> CreateConnectedAccountAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(SimulatedIds.New("acct"));

    public Task<string> CreateOnboardingLinkAsync(
        string accountId,
        string refreshUrl,
        string returnUrl,
        CancellationToken cancellationToken) => Task.FromResult(returnUrl);
}
