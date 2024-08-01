using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;

namespace Lodge.Application.Bookings.Reserve;

/// <summary>
/// Represents the <see cref="BookingReservedDomainEvent"/> handler.
/// </summary>
/// <param name="integrationEventPublisher">The integration event publisher.</param>
internal sealed class PublishIntegrationEventOnBookingReservedEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<BookingReservedDomainEvent>
{
    /// <inheritdoc />
    public Task Handle(BookingReservedDomainEvent notification, CancellationToken cancellationToken)
    {
        integrationEventPublisher.Publish(new BookingReservedIntegrationEvent(notification));

        return Task.CompletedTask;
    }
}
