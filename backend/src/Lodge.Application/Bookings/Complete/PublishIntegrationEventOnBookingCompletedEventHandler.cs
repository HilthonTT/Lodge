using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;

namespace Lodge.Application.Bookings.Complete;

internal sealed class PublishIntegrationEventOnBookingCompletedEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<BookingCompletedDomainEvent>
{
    public Task Handle(BookingCompletedDomainEvent notification, CancellationToken cancellationToken) =>
        integrationEventPublisher.PublishAsync(new BookingCompletedIntegrationEvent(notification.BookingId), cancellationToken);
}
