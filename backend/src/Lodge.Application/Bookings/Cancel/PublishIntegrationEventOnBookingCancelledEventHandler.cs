using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;

namespace Lodge.Application.Bookings.Cancel;

/// <summary>
/// Represents the <see cref="BookingCancelledDomainEvent"/> handler.
/// </summary>
/// <param name="integrationEventPublisher">The integration event publisher.</param>
internal sealed class PublishIntegrationEventOnBookingCancelledEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<BookingCancelledDomainEvent>
{
    public Task Handle(BookingCancelledDomainEvent notification, CancellationToken cancellationToken)
    {
        integrationEventPublisher.Publish(new BookingCancelledIntegrationEvent(notification));

        return Task.CompletedTask;
    }
}
