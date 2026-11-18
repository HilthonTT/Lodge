using Lodge.Domain.Core.Events;

namespace Lodge.Domain.Payouts.Events;

/// <summary>
/// Raised when a host's payout account becomes active (payouts enabled), so that payouts on hold are retried.
/// </summary>
/// <param name="UserId">The host's user identifier.</param>
public sealed record PayoutAccountActivatedDomainEvent(Guid UserId) : IDomainEvent;
