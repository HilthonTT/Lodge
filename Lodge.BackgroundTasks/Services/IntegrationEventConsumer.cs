using Lodge.Application.Abstractions.Messaging;
using MediatR;

namespace Lodge.BackgroundTasks.Services;

/// <summary>
/// Represents the integration event consumer.
/// </summary>
/// <param name="sender">The sender.</param>
internal sealed class IntegrationEventConsumer(ISender sender) : IIntegrationEventConsumer
{
    /// <inheritdoc />
    public void Consume(IIntegrationEvent integrationEvent)
    {
        sender.Send(integrationEvent).GetAwaiter().GetResult();
    }
}
