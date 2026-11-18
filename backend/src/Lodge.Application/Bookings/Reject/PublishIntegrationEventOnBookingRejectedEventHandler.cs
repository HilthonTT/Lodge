using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;

namespace Lodge.Application.Bookings.Reject;

internal sealed class PublishIntegrationEventOnBookingRejectedEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<BookingRejectedDomainEvent>
{
    public Task Handle(BookingRejectedDomainEvent notification, CancellationToken cancellationToken) =>
        integrationEventPublisher.PublishAsync(new BookingRejectedIntegrationEvent(notification.BookingId), cancellationToken);
}
