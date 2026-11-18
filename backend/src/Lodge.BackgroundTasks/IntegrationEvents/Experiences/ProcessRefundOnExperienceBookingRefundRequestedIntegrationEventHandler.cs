using Lodge.Application.Notifications;
using Lodge.Application.Payments;
using Lodge.BackgroundTasks.Abstractions.Messaging;

namespace Lodge.BackgroundTasks.IntegrationEvents.Experiences;

/// <summary>
/// Sends the refund of a cancelled, paid experience booking (Stripe Refunds API, or simulated) and records its outcome.
/// </summary>
internal sealed class ProcessRefundOnExperienceBookingRefundRequestedIntegrationEventHandler(IRefundProcessor refundProcessor)
    : IIntegrationEventHandler<ExperienceBookingRefundRequestedIntegrationEvent>
{
    public Task Handle(ExperienceBookingRefundRequestedIntegrationEvent notification, CancellationToken cancellationToken) =>
        refundProcessor.ProcessExperienceAsync(notification.ExperienceBookingId, cancellationToken);
}
