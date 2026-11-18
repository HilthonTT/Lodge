using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;

namespace Lodge.Application.Payments;

internal sealed class PublishIntegrationEventOnBookingRefundRequestedDomainEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<BookingRefundRequestedDomainEvent>
{
    public Task Handle(BookingRefundRequestedDomainEvent notification, CancellationToken cancellationToken) =>
        integrationEventPublisher.PublishAsync(
            new BookingRefundRequestedIntegrationEvent(notification.BookingId),
            cancellationToken);
}
