using Lodge.Application.Stripe;
using Stripe;

namespace Lodge.Application.Payments;

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
