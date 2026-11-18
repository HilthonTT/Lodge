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
        ArgumentNullException.ThrowIfNull(bookingCancelledEmail);

        string what = bookingCancelledEmail.ListingName is null
            ? "Your booking"
            : $"Your booking at {bookingCancelledEmail.ListingName}";

        string who = bookingCancelledEmail.CancelledByHost ? " by the host" : string.Empty;

        string refund = bookingCancelledEmail.RefundAmount switch
        {
            null => string.Empty,
            0 => " No refund applies under the cancellation policy.",
            decimal amount when bookingCancelledEmail.RefundIsManual =>
                $" A refund of {FormatAmount(amount, bookingCancelledEmail.Currency)} will be arranged by our team.",
            decimal amount =>
                $" You will be refunded {FormatAmount(amount, bookingCancelledEmail.Currency)} to your original payment method.",
        };

        var mailRequest = new MailRequest(
              bookingCancelledEmail.EmailTo,
              "Booking cancelled ❌",
              $"Hello {bookingCancelledEmail.Name}," +
              Environment.NewLine +
              Environment.NewLine +
              $"{what} has been cancelled{who}.{refund}");

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

    private static string FormatAmount(decimal amount, string? currency) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{amount:0.00} {currency}").TrimEnd();

    /// <inheritdoc />
    public Task SendNewMessageEmailAsync(NewMessageEmail newMessageEmail, CancellationToken cancellationToken = default)
    {
        var mailRequest = new MailRequest(
            newMessageEmail.EmailTo,
            $"New message from {newMessageEmail.SenderFirstName} 💬",
            $"Hello {newMessageEmail.Name}," +
            Environment.NewLine +
            Environment.NewLine +
            $"{newMessageEmail.SenderFirstName} sent you a message about {newMessageEmail.ListingName}:" +
            Environment.NewLine +
            Environment.NewLine +
            $"\"{newMessageEmail.Preview}\"" +
            Environment.NewLine +
            Environment.NewLine +
            $"Reply: {newMessageEmail.Link}");

        return emailService.SendEmailAsync(mailRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendTripReminderEmailAsync(TripReminderEmail tripReminderEmail, CancellationToken cancellationToken = default)
    {
        string instructions = string.IsNullOrWhiteSpace(tripReminderEmail.CheckInInstructions)
            ? string.Empty
            : Environment.NewLine + Environment.NewLine + "Check-in instructions:" + Environment.NewLine +
              tripReminderEmail.CheckInInstructions;

        var mailRequest = new MailRequest(
            tripReminderEmail.EmailTo,
            $"Your trip to {tripReminderEmail.ListingName} is coming up 🧳",
            $"Hello {tripReminderEmail.Name}," +
            Environment.NewLine +
            Environment.NewLine +
            $"Check-in at {tripReminderEmail.ListingName} is on {tripReminderEmail.CheckIn:yyyy-MM-dd}." +
            instructions +
            Environment.NewLine +
            Environment.NewLine +
            $"Trip details: {tripReminderEmail.Link}");

        return emailService.SendEmailAsync(mailRequest, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendReviewReminderEmailAsync(ReviewReminderEmail reviewReminderEmail, CancellationToken cancellationToken = default)
    {
        string ask = reviewReminderEmail.ForHost
            ? $"How was your guest at {reviewReminderEmail.ListingName}? Leave a review for them"
            : $"How was your stay at {reviewReminderEmail.ListingName}? Leave a review";

        var mailRequest = new MailRequest(
            reviewReminderEmail.EmailTo,
            "Share your experience ⭐",
            $"Hello {reviewReminderEmail.Name}," +
            Environment.NewLine +
            Environment.NewLine +
            $"{ask} by {reviewReminderEmail.ReviewDeadline:yyyy-MM-dd}." +
            Environment.NewLine +
            Environment.NewLine +
            reviewReminderEmail.Link);

        return emailService.SendEmailAsync(mailRequest, cancellationToken);
    }
}
