using Lodge.Application.Abstractions.Messaging;
using RabbitMQ.Client;

namespace Lodge.Infrastructure.Messaging;

/// <summary>
/// Publishes integration events as persistent messages to the integration event queue.
/// </summary>
internal sealed class IntegrationEventPublisher(RabbitMqConnectionProvider connectionProvider) : IIntegrationEventPublisher
{
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await using IChannel channel = await connectionProvider.CreateChannelAsync(cancellationToken);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = IntegrationEventSerializer.GetTypeName(integrationEvent),
            MessageId = Guid.NewGuid().ToString(),
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: connectionProvider.Options.QueueName,
            mandatory: false,
            basicProperties: properties,
            body: IntegrationEventSerializer.Serialize(integrationEvent),
            cancellationToken: cancellationToken);
    }
}
