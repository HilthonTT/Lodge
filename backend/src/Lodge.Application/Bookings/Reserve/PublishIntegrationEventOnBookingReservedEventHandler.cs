using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;

namespace Lodge.Application.Bookings.Reserve;

internal sealed class PublishIntegrationEventOnBookingReservedEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<BookingReservedDomainEvent>
{
    public Task Handle(BookingReservedDomainEvent notification, CancellationToken cancellationToken) =>
        integrationEventPublisher.PublishAsync(new BookingReservedIntegrationEvent(notification.BookingId), cancellationToken);
}
