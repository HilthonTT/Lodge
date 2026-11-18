using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Users.Events;

namespace Lodge.Application.Users.Create;

internal sealed class PublishIntegrationEventOnUserCreatedDomainEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<UserCreatedDomainEvent>
{
    public Task Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken) =>
        integrationEventPublisher.PublishAsync(new UserCreatedIntegrationEvent(notification.UserId), cancellationToken);
}
