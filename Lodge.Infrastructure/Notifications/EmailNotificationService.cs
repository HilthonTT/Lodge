using Lodge.Application.Abstractions.Emails;
using Lodge.Application.Abstractions.Notifications;
using Lodge.Contracts.Emails;

namespace Lodge.Infrastructure.Notifications;

/// <summary>
/// Represents the email notification service.
/// </summary>
/// <param name="emailService">The email service.</param>
internal sealed class EmailNotificationService(IEmailService emailService) :
    IEmailNotificationService
{
    /// <inheritdoc />
    public Task SendNotificationEmailAsync(
        NotifcationEmail notifcationEmail,
        CancellationToken cancellationToken = default)
    {
        var mailRequest = new MailRequest(notifcationEmail.EmailTo, notifcationEmail.Subject, notifcationEmail.Body);

        return emailService.SendEmailAsync(mailRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendPasswordChangedEmailAsync(
        PasswordChangedEmail passwordChangedEmail, 
        CancellationToken cancellationToken = default)
    {
        var mailRequest = new MailRequest(
                passwordChangedEmail.EmailTo,
                "Password changed 🔐",
                $"Hello {passwordChangedEmail.Name}," +
                Environment.NewLine +
                Environment.NewLine +
                "Your password was successfully changed.");

        return emailService.SendEmailAsync(mailRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendWelcomeEmailAsync(
        WelcomeEmail welcomeEmail, 
        CancellationToken cancellationToken = default)
    {
        var mailRequest = new MailRequest(
                welcomeEmail.EmailTo,
                "Welcome to Lodge! 🎉",
                $"Welcome to Lodge {welcomeEmail.Name}," +
                Environment.NewLine +
                Environment.NewLine +
                $"You have registered with the email {welcomeEmail.EmailTo}.");

        return emailService.SendEmailAsync(mailRequest, cancellationToken);
    }
}
