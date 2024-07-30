using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Users.Events;

namespace Lodge.Application.Users.ChangePassword;

/// <summary>
/// Represents the <see cref="UserPasswordChangedDomainEvent"/> handler.
/// </summary>
/// <param name="integrationEventPublisher">The integration event publisher.</param>
internal sealed class PublishIntegrationEventOnUserPasswordChangedDomainEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<UserPasswordChangedDomainEvent>
{
    /// <inheritdoc />
    public Task Handle(UserPasswordChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        integrationEventPublisher.Publish(new UserPasswordChangedIntegrationEvent(notification));

        return Task.CompletedTask;
    }
}
