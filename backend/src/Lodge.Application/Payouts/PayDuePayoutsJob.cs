using Lodge.Application.Abstractions.Data;
using Lodge.Application.Payments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Payouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StripeException = Stripe.StripeException;

namespace Lodge.Application.Payouts;

internal sealed partial class PayDuePayoutsJob(
    IDbContext context,
    IUnitOfWork unitOfWork,
    PaymentGateways paymentGateways,
    IDateTimeProvider dateTimeProvider,
    ILogger<PayDuePayoutsJob> logger) : IPayDuePayoutsJob
{
    public Task<int> PayDueAsync(CancellationToken cancellationToken = default) => PayAsync(null, cancellationToken);

    public Task<int> PayDueForHostAsync(Guid hostId, CancellationToken cancellationToken = default) =>
        PayAsync(hostId, cancellationToken);

    private async Task<int> PayAsync(Guid? hostId, CancellationToken cancellationToken)
    {
        DateTime utcNow = dateTimeProvider.UtcNow;
        DateOnly today = DateOnly.FromDateTime(utcNow);

        IQueryable<Payout> query = context.Set<Payout>()
            .Where(payout => (payout.Status == PayoutStatus.Scheduled || payout.Status == PayoutStatus.OnHold) &&
                             payout.ScheduledFor <= today);

        if (hostId.HasValue)
        {
            query = query.Where(payout => payout.HostId == hostId.Value);
        }

        List<Payout> payouts = await query.OrderBy(payout => payout.ScheduledFor).ToListAsync(cancellationToken);

        if (payouts.Count == 0)
        {
            return 0;
        }

        Guid[] hostIds = [.. payouts.Select(payout => payout.HostId).Distinct()];

        Dictionary<Guid, PayoutAccount> accounts = await context.Set<PayoutAccount>()
            .Where(account => hostIds.Contains(account.UserId))
            .ToDictionaryAsync(account => account.UserId, cancellationToken);

        int paid = 0;

        foreach (Payout payout in payouts)
        {
            if (!accounts.TryGetValue(payout.HostId, out PayoutAccount? account) || !account.CanReceivePayouts)
            {
                payout.PutOnHold();
                continue;
            }

            try
            {
                string transferId = await paymentGateways.For(account.ExternalAccountId).TransferAsync(
                    payout.Amount.Amount,
                    payout.Amount.Currency.Code,
                    account.ExternalAccountId,
                    payout.Id.ToString(),
                    cancellationToken);

                payout.MarkPaid(transferId, utcNow);
                paid++;
            }
            catch (StripeException exception)
            {
                LogPayoutFailed(logger, exception, payout.Id);
                payout.MarkFailed(exception.Message);
            }

            // Saved after every transfer, so a crash never pays a payout twice (the idempotency key helps too).
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogPaid(logger, paid, payouts.Count);

        return paid;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Payout {PayoutId} failed")]
    private static partial void LogPayoutFailed(ILogger logger, Exception exception, Guid payoutId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Paid {Paid} of {Due} due payouts")]
    private static partial void LogPaid(ILogger logger, int paid, int due);
}
