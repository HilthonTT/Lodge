using Lodge.Domain.Payouts;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the payout repository.
/// </summary>
internal sealed class PayoutRepository(LodgeDbContext context) : IPayoutRepository
{
    public Task<Payout?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        context.Payouts.FirstOrDefaultAsync(payout => payout.BookingId == bookingId, cancellationToken);

    public Task<Payout?> GetByExperienceBookingIdAsync(Guid experienceBookingId, CancellationToken cancellationToken = default) =>
        context.Payouts.FirstOrDefaultAsync(payout => payout.ExperienceBookingId == experienceBookingId, cancellationToken);

    public Task<PayoutAccount?> GetAccountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.PayoutAccounts.FirstOrDefaultAsync(account => account.UserId == userId, cancellationToken);

    public Task<PayoutAccount?> GetAccountByExternalIdAsync(
        string externalAccountId,
        CancellationToken cancellationToken = default) =>
        context.PayoutAccounts.FirstOrDefaultAsync(account => account.ExternalAccountId == externalAccountId, cancellationToken);

    public void Insert(Payout payout) => context.Payouts.Add(payout);

    public void InsertAccount(PayoutAccount account) => context.PayoutAccounts.Add(account);
}
