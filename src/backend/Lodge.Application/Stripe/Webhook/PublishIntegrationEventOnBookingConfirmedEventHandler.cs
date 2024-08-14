using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;

namespace Lodge.Application.Bookings.Confirm;

/// <summary>
/// Represents the <see cref="BookingConfirmedDomainEvent"/> handler.
/// </summary>
/// <param name="integrationEventPublisher">The integration event publisher.</param>
internal class PublishIntegrationEventOnBookingConfirmedEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<BookingConfirmedDomainEvent>
{
    /// <inheritdoc />
    public Task Handle(BookingConfirmedDomainEvent notification, CancellationToken cancellationToken)
    {
        integrationEventPublisher.Publish(new BookingConfirmedIntegrationEvent(notification));

        return Task.CompletedTask;
    }
}
