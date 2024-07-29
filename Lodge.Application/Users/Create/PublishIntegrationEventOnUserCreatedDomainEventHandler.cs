using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Users.Events;

namespace Lodge.Application.Users.Create;

/// <summary>
/// Represents the <see cref="UserCreatedDomainEvent"/> handler.
/// </summary>
/// <param name="integrationEventPublisher">The integration event publisher.</param>
internal sealed class PublishIntegrationEventOnUserCreatedDomainEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<UserCreatedDomainEvent>
{
    /// <inheritdoc />
    public Task Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        integrationEventPublisher.Publish(new UserCreatedIntegrationEvent(notification));

        return Task.CompletedTask;
    }
}
