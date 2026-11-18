using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Users.Events;

namespace Lodge.Application.Users.ChangePassword;

internal sealed class PublishIntegrationEventOnUserPasswordChangedDomainEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<UserPasswordChangedDomainEvent>
{
    public Task Handle(UserPasswordChangedDomainEvent notification, CancellationToken cancellationToken) =>
        integrationEventPublisher.PublishAsync(
            new UserPasswordChangedIntegrationEvent(notification.UserId),
            cancellationToken);
}
