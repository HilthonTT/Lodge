namespace Lodge.Domain.Payouts;

/// <summary>
/// Represents the repository of payouts and payout accounts.
/// </summary>
public interface IPayoutRepository
{
    Task<Payout?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<Payout?> GetByExperienceBookingIdAsync(Guid experienceBookingId, CancellationToken cancellationToken = default);

    Task<PayoutAccount?> GetAccountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PayoutAccount?> GetAccountByExternalIdAsync(string externalAccountId, CancellationToken cancellationToken = default);

    void Insert(Payout payout);

    void InsertAccount(PayoutAccount account);
}
