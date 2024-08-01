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
}
