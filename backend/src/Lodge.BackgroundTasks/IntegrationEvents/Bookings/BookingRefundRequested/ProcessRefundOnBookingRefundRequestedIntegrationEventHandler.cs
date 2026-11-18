using Lodge.Application.Payments;
using Lodge.BackgroundTasks.Abstractions.Messaging;

namespace Lodge.BackgroundTasks.IntegrationEvents.Bookings.BookingRefundRequested;

/// <summary>
/// Sends the refund of a cancelled, paid booking (Stripe Refunds API, or simulated) and records its outcome.
/// </summary>
internal sealed class ProcessRefundOnBookingRefundRequestedIntegrationEventHandler(IRefundProcessor refundProcessor)
    : IIntegrationEventHandler<BookingRefundRequestedIntegrationEvent>
{
    public Task Handle(BookingRefundRequestedIntegrationEvent notification, CancellationToken cancellationToken) =>
        refundProcessor.ProcessAsync(notification.BookingId, cancellationToken);
}
