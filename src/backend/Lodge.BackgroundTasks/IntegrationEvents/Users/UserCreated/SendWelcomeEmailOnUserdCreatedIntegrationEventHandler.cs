using Lodge.Application.Abstractions.Notifications;
using Lodge.Application.Users.Create;
using Lodge.BackgroundTasks.Abstractions.Messaging;
using Lodge.Contracts.Emails;
using Lodge.Domain.Core.Exceptions;
using Lodge.Domain.Users;

namespace Lodge.BackgroundTasks.IntegrationEvents.Users.UserCreated;

/// <summary>
/// Represents <see cref="UserCreatedIntegrationEvent"/> handler.
/// </summary>
/// <param name="userRepository">The user repository.</param>
/// <param name="emailNotificationService">The email notification service.</param>
internal sealed class SendWelcomeEmailOnUserCreatedIntegrationEventHandler(
    IUserRepository userRepository,
    IEmailNotificationService emailNotificationService) : IIntegrationEventHandler<UserCreatedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(UserCreatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(notification.UserId, cancellationToken)
            ?? throw new DomainException(UserErrors.NotFound(notification.UserId));

        var welcomeEmail = new WelcomeEmail(user.Email, user.FullName);

        await emailNotificationService.SendWelcomeEmailAsync(welcomeEmail, cancellationToken);
    }
}
