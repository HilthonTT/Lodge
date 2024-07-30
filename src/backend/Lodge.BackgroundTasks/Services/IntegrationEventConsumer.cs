using Lodge.Application.Abstractions.Messaging;
using MediatR;

namespace Lodge.BackgroundTasks.Services;

/// <summary>
/// Represents the integration event consumer.
/// </summary>
/// <param name="publisher">The publisher.</param>
internal sealed class IntegrationEventConsumer(IPublisher publisher) : IIntegrationEventConsumer
{
    /// <inheritdoc />
    public void Consume(IIntegrationEvent integrationEvent)
    {
        publisher.Publish(integrationEvent).GetAwaiter().GetResult();
    }
}
