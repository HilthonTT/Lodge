using Lodge.Application.Abstractions.Notifications;
using Lodge.Application.Users.ChangePassword;
using Lodge.BackgroundTasks.Abstractions.Messaging;
using Lodge.Contracts.Emails;
using Lodge.Domain.Core.Exceptions;
using Lodge.Domain.Users;

namespace Lodge.BackgroundTasks.IntegrationEvents.Users.UserPasswordChanged;

/// <summary>
/// Represents the <see cref="UserPasswordChangedIntegrationEvent"/> handler.
/// </summary>
/// <param name="userRepository">The user repository.</param>
/// <param name="emailNotificationService">The email notification service.</param>
internal sealed class NotifyUserOnPasswordChangedIntegrationEventHandler(
    IUserRepository userRepository,
    IEmailNotificationService emailNotificationService) : IIntegrationEventHandler<UserPasswordChangedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(UserPasswordChangedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(notification.UserId, cancellationToken) 
            ?? throw new DomainException(UserErrors.NotFound(notification.UserId));

        var passwordChangedEmail = new PasswordChangedEmail(user.Email, user.FullName);

        await emailNotificationService.SendPasswordChangedEmailAsync(passwordChangedEmail, cancellationToken);
    }
}
