using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;

namespace Lodge.Application.Bookings.Confirm;

internal sealed class PublishIntegrationEventOnBookingConfirmedEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<BookingConfirmedDomainEvent>
{
    public Task Handle(BookingConfirmedDomainEvent notification, CancellationToken cancellationToken) =>
        integrationEventPublisher.PublishAsync(new BookingConfirmedIntegrationEvent(notification.BookingId), cancellationToken);
}
