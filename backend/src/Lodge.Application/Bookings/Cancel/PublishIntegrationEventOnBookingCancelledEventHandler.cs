using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;

namespace Lodge.Application.Bookings.Cancel;

internal sealed class PublishIntegrationEventOnBookingCancelledEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<BookingCancelledDomainEvent>
{
    public Task Handle(BookingCancelledDomainEvent notification, CancellationToken cancellationToken) =>
        integrationEventPublisher.PublishAsync(new BookingCancelledIntegrationEvent(notification.BookingId), cancellationToken);
}
