using Lodge.Application.Abstractions.Messaging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.Api.FunctionalTests.Abstractions;

/// <summary>
/// Replaces RabbitMQ: integration events are dispatched right away to the BackgroundTasks handlers, in a new scope
/// (like the consumer does). Together with running the outbox job, tests exercise the whole asynchronous pipeline:
/// domain event → outbox → integration event → handler.
/// </summary>
public sealed class InProcessIntegrationEventPublisher(IServiceScopeFactory scopeFactory) : IIntegrationEventPublisher
{
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IPublisher>().Publish(integrationEvent, cancellationToken);
    }
}
