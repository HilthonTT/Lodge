using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Payouts.Events;

namespace Lodge.Application.Payouts;

/// <summary>
/// Retries the payouts on hold once the host's payout account becomes active.
/// </summary>
internal sealed class PayHeldPayoutsOnPayoutAccountActivatedDomainEventHandler(IPayDuePayoutsJob payDuePayoutsJob)
    : IDomainEventHandler<PayoutAccountActivatedDomainEvent>
{
    public Task Handle(PayoutAccountActivatedDomainEvent notification, CancellationToken cancellationToken) =>
        payDuePayoutsJob.PayDueForHostAsync(notification.UserId, cancellationToken);
}
