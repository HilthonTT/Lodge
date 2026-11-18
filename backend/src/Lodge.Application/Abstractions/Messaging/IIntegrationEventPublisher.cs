namespace Lodge.Application.Abstractions.Messaging;

/// <summary>
/// Publishes integration events to the message broker.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
