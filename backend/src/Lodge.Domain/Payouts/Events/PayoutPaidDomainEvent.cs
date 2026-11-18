using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Payouts.Events;

/// <summary>
/// Raised when a payout has been sent to the host.
/// </summary>
/// <param name="PayoutId">The payout identifier.</param>
public sealed record PayoutPaidDomainEvent(Guid PayoutId) : IDomainEvent;
