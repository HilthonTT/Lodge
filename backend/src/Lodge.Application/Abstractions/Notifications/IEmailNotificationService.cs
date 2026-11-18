using Lodge.Contracts.Emails;

namespace Lodge.Application.Abstractions.Notifications;

/// <summary>
/// Represents the email notification service interface.
/// </summary>
public interface IEmailNotificationService
{
    /// <summary>
    /// Sends the welcome email notification based on the specified request.
    /// </summary>
    /// <param name="welcomeEmail">The welcome email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task SendWelcomeEmailAsync(WelcomeEmail welcomeEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the password changed email.
    /// </summary>
    /// <param name="passwordChangedEmail">The password changed email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task SendPasswordChangedEmailAsync(PasswordChangedEmail passwordChangedEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the notification email.
    /// </summary>
    /// <param name="notificationEmail">The notification email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task SendNotificationEmailAsync(NotificationEmail notificationEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the booking reserved email.
    /// </summary>
    /// <param name="bookingReservedEmail">The booking reserved email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task SendBookingReservedEmailAsync(BookingReservedEmail bookingReservedEmail, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends the booking confirmed email.
    /// </summary>
    /// <param name="bookingConfirmedEmail">The booking confirmed email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task SendBookingConfirmedEmailAsync(BookingConfirmedEmail bookingConfirmedEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the booking rejected email.
    /// </summary>
    /// <param name="BookingRejectedEmail">The booking rejected email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task SendBookingRejectedEmailAsync(BookingRejectedEmail bookingRejectedEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the booking completed email.
    /// </summary>
    /// <param name="bookingCompletedEmail">The booking completed email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task SendBookingCompletedEmailAsync(BookingCompletedEmail bookingCompletedEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the booking cancelled email.
    /// </summary>
    /// <param name="bookingCancelledEmail">The booking cancelled email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task SendBookingCancelledEmailAsync(BookingCancelledEmail bookingCancelledEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the e-mail with the e-mail verification link.
    /// </summary>
    /// <param name="emailVerificationEmail">The e-mail verification email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task SendEmailVerificationEmailAsync(EmailVerificationEmail emailVerificationEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the e-mail with the password reset link.
    /// </summary>
    /// <param name="passwordResetEmail">The password reset email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task SendPasswordResetEmailAsync(PasswordResetEmail passwordResetEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the "new message" e-mail.
    /// </summary>
    Task SendNewMessageEmailAsync(NewMessageEmail newMessageEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the trip reminder (two days before check-in).
    /// </summary>
    Task SendTripReminderEmailAsync(TripReminderEmail tripReminderEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the review reminder (the day after check-out).
    /// </summary>
    Task SendReviewReminderEmailAsync(ReviewReminderEmail reviewReminderEmail, CancellationToken cancellationToken = default);
}
