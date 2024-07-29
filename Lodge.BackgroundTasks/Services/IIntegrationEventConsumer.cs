using Lodge.Application.Abstractions.Messaging;

namespace Lodge.BackgroundTasks.Services;

/// <summary>
/// Represents the integration event consumer interface.
/// </summary>
public interface IIntegrationEventConsumer
{
    /// <summary>
    /// Consumes the incoming specified integration event.
    /// </summary>
    /// <param name="integrationEvent">The integration event.</param>
    void Consume(IIntegrationEvent integrationEvent);
}
