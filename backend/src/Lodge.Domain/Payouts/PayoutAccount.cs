using Lodge.Domain.Core.Abstractions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Payouts.Events;

namespace Lodge.Domain.Payouts;

/// <summary>
/// A host's payout account: a Stripe Connect Express account, or a simulated one (sim_acct_...).
/// </summary>
public sealed class PayoutAccount : Entity, IAuditableEntity
{
    private PayoutAccount(Guid id, Guid userId, string externalAccountId)
        : base(id)
    {
        UserId = userId;
        ExternalAccountId = externalAccountId;
        Status = PayoutAccountStatus.Pending;
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private PayoutAccount()
    {
    }

    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the Stripe connected account id (acct_...) or a simulated id (sim_acct_...).
    /// </summary>
    public string ExternalAccountId { get; private set; } = string.Empty;

    public PayoutAccountStatus Status { get; private set; }

    public bool PayoutsEnabled { get; private set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public bool CanReceivePayouts => Status == PayoutAccountStatus.Active && PayoutsEnabled;

    /// <summary>
    /// Creates a payout account whose onboarding has started (<see cref="PayoutAccountStatus.Pending"/>).
    /// </summary>
    public static PayoutAccount Create(Guid userId, string externalAccountId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalAccountId);

        return new PayoutAccount(Guid.NewGuid(), userId, externalAccountId);
    }

    /// <summary>
    /// Updates the account from the provider (Stripe <c>account.updated</c>, or immediately when simulated). Raises
    /// <see cref="PayoutAccountActivatedDomainEvent"/> when payouts become enabled.
    /// </summary>
    public void Update(bool payoutsEnabled)
    {
        bool wasActive = Status == PayoutAccountStatus.Active;

        PayoutsEnabled = payoutsEnabled;
        Status = payoutsEnabled ? PayoutAccountStatus.Active : PayoutAccountStatus.Pending;

        if (!wasActive && Status == PayoutAccountStatus.Active)
        {
            RaiseDomainEvent(new PayoutAccountActivatedDomainEvent(UserId));
        }
    }
}
