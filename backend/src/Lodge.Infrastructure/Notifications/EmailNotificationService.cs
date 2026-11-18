using Lodge.Application.Abstractions.Emails;
using Lodge.Application.Abstractions.Notifications;
using Lodge.Contracts.Emails;

namespace Lodge.Infrastructure.Notifications;

/// <summary>
/// Represents the email notification service.
/// </summary>
/// <param name="emailService">The email service.</param>
internal sealed class EmailNotificationService(IEmailService emailService) : IEmailNotificationService
{
    /// <inheritdoc />
    public Task SendNotificationEmailAsync(
        NotificationEmail notificationEmail,
        CancellationToken cancellationToken = default)
    {
        var mailRequest = new MailRequest(notificationEmail.EmailTo, notificationEmail.Subject, notificationEmail.Body);

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

    /// <inheritdoc />
    public Task SendBookingReservedEmailAsync(
        BookingReservedEmail bookingReservedEmail,
        CancellationToken cancellationToken = default)
    {
        var mailRequest = new MailRequest(
               bookingReservedEmail.EmailTo,
               "Booking reserved 📖",
               $"Hello {bookingReservedEmail.Name}," +
               Environment.NewLine +
               Environment.NewLine +
               "You've reserved an apartment. Complete the payment to confirm your booking.");

        return emailService.SendEmailAsync(mailRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendBookingConfirmedEmailAsync(
        BookingConfirmedEmail bookingConfirmedEmail,
        CancellationToken cancellationToken = default)
    {
        var mailRequest = new MailRequest(
               bookingConfirmedEmail.EmailTo,
               "Booking confirmed ✅",
               $"Hello {bookingConfirmedEmail.Name}," +
               Environment.NewLine +
               Environment.NewLine +
               "Your booking has now been confirmed!");

        return emailService.SendEmailAsync(mailRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendBookingRejectedEmailAsync(
        BookingRejectedEmail bookingRejectedEmail,
        CancellationToken cancellationToken = default)
    {
        var mailRequest = new MailRequest(
               bookingRejectedEmail.EmailTo,
               "Booking rejected ❌",
               $"Hello {bookingRejectedEmail.Name}," +
               Environment.NewLine +
               Environment.NewLine +
               "Your booking has been rejected...");

        return emailService.SendEmailAsync(mailRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendBookingCompletedEmailAsync(
        BookingCompletedEmail bookingCompletedEmail,
        CancellationToken cancellationToken = default)
    {
        var mailRequest = new MailRequest(
              bookingCompletedEmail.EmailTo,
              "Booking completed 👍",
              $"Hello {bookingCompletedEmail.Name}," +
              Environment.NewLine +
              Environment.NewLine +
              "Your booking has been completed!");

        return emailService.SendEmailAsync(mailRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendBookingCancelledEmailAsync(
        BookingCancelledEmail bookingCancelledEmail,
        CancellationToken cancellationToken = default)
    {
        var mailRequest = new MailRequest(
              bookingCancelledEmail.EmailTo,
              "Booking cancelled ❌",
              $"Hello {bookingCancelledEmail.Name}," +
              Environment.NewLine +
              Environment.NewLine +
              "Your booking has been cancelled.");

        return emailService.SendEmailAsync(mailRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendEmailVerificationEmailAsync(
        EmailVerificationEmail emailVerificationEmail,
        CancellationToken cancellationToken = default)
    {
        var mailRequest = new MailRequest(
              emailVerificationEmail.EmailTo,
              "Verify your e-mail address ✉️",
              $"Hello {emailVerificationEmail.Name}," +
              Environment.NewLine +
              Environment.NewLine +
              "Please confirm your e-mail address by opening this link (valid for 24 hours):" +
              Environment.NewLine +
              Environment.NewLine +
              emailVerificationEmail.Link +
              Environment.NewLine +
              Environment.NewLine +
              "If you did not create a Lodge account, you can ignore this e-mail.");

        return emailService.SendEmailAsync(mailRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendPasswordResetEmailAsync(
        PasswordResetEmail passwordResetEmail,
        CancellationToken cancellationToken = default)
    {
        var mailRequest = new MailRequest(
              passwordResetEmail.EmailTo,
              "Reset your password 🔑",
              $"Hello {passwordResetEmail.Name}," +
              Environment.NewLine +
              Environment.NewLine +
              "Open this link to choose a new password (valid for 1 hour):" +
              Environment.NewLine +
              Environment.NewLine +
              passwordResetEmail.Link +
              Environment.NewLine +
              Environment.NewLine +
              "If you did not ask to reset your password, you can ignore this e-mail.");

        return emailService.SendEmailAsync(mailRequest, cancellationToken);
    }
}
