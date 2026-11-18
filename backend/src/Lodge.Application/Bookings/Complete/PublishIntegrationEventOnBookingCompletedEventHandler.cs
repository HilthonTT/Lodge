using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;

namespace Lodge.Application.Bookings.Complete;

/// <summary>
/// Represents the <see cref="BookingCompletedDomainEvent"/> handler.
/// </summary>
/// <param name="integrationEventPublisher">The integration event publisher.</param>
internal sealed class PublishIntegrationEventOnBookingCompletedEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<BookingCompletedDomainEvent>
{
    public Task Handle(BookingCompletedDomainEvent notification, CancellationToken cancellationToken)
    {
        integrationEventPublisher.Publish(new BookingCompletedIntegrationEvent(notification));

        return Task.CompletedTask;
    }
}
