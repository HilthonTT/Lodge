using Lodge.Application.Stripe;
using Stripe;

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

/// <summary>
/// Ids created in simulated mode start with <see cref="Prefix"/>; money tied to them never goes through Stripe.
/// </summary>
internal static class SimulatedIds
{
    public const string Prefix = "sim_";

    public static string New(string kind) => $"{Prefix}{kind}_{Guid.NewGuid():N}";

    public static bool IsSimulated(string? id) => id?.StartsWith(Prefix, StringComparison.Ordinal) == true;
}

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

/// <summary>
/// Calls the Stripe Refunds, Transfers, Accounts and Account Links APIs. Idempotency keys make retries safe.
/// </summary>
internal sealed class StripePaymentGateway(StripeClientProvider stripeClientProvider) : IPaymentGateway
{
    public async Task<string> RefundAsync(
        string paymentIntentId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var service = new RefundService(stripeClientProvider.Client);

        Refund refund = await service.CreateAsync(
            new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = ToMinorUnits(amount),
            },
            new RequestOptions { IdempotencyKey = $"refund-{idempotencyKey}" },
            cancellationToken);

        return refund.Id;
    }

    public async Task<string> TransferAsync(
        decimal amount,
        string currency,
        string destinationAccountId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var service = new TransferService(stripeClientProvider.Client);

        Transfer transfer = await service.CreateAsync(
            new TransferCreateOptions
            {
                Amount = ToMinorUnits(amount),
                Currency = currency.ToLowerInvariant(),
                Destination = destinationAccountId,
            },
            new RequestOptions { IdempotencyKey = $"payout-{idempotencyKey}" },
            cancellationToken);

        return transfer.Id;
    }

    public async Task<string> CreateConnectedAccountAsync(string email, CancellationToken cancellationToken)
    {
        var service = new AccountService(stripeClientProvider.Client);

        Account account = await service.CreateAsync(
            new AccountCreateOptions
            {
                Type = "express",
                Email = email,
                Capabilities = new AccountCapabilitiesOptions
                {
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                },
            },
            cancellationToken: cancellationToken);

        return account.Id;
    }

    public async Task<string> CreateOnboardingLinkAsync(
        string accountId,
        string refreshUrl,
        string returnUrl,
        CancellationToken cancellationToken)
    {
        var service = new AccountLinkService(stripeClientProvider.Client);

        AccountLink link = await service.CreateAsync(
            new AccountLinkCreateOptions
            {
                Account = accountId,
                RefreshUrl = refreshUrl,
                ReturnUrl = returnUrl,
                Type = "account_onboarding",
            },
            cancellationToken: cancellationToken);

        return link.Url;
    }

    /// <summary>
    /// Stripe expects amounts in the smallest currency unit (cents for USD and EUR).
    /// </summary>
    internal static long ToMinorUnits(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}

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
