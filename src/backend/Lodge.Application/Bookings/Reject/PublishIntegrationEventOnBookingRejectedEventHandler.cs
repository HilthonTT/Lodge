using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;

namespace Lodge.Application.Bookings.Reject;

/// <summary>
/// Represents the <see cref="BookingRejectedDomainEvent"/> handler.
/// </summary>
/// <param name="integrationEventPublisher"></param>
internal sealed class PublishIntegrationEventOnBookingRejectedEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<BookingRejectedDomainEvent>
{
    /// <inheritdoc />
    public Task Handle(BookingRejectedDomainEvent notification, CancellationToken cancellationToken)
    {
        integrationEventPublisher.Publish(new BookingRejectedIntegrationEvent(notification));

        return Task.CompletedTask;
    }
}
