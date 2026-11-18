using System.Collections.Concurrent;
using Lodge.Application.Abstractions.Notifications;
using Lodge.Contracts.Emails;

namespace Lodge.Api.FunctionalTests.Abstractions;

/// <summary>
/// Records e-mails instead of sending them, so tests can read verification and reset links.
/// </summary>
public sealed class RecordingEmailNotificationService : IEmailNotificationService
{
    private readonly ConcurrentQueue<object> _emails = new();

    public IReadOnlyList<EmailVerificationEmail> VerificationEmails(string emailTo) =>
        _emails.OfType<EmailVerificationEmail>().Where(e => e.EmailTo == emailTo).ToList();

    public IReadOnlyList<PasswordResetEmail> PasswordResetEmails(string emailTo) =>
        _emails.OfType<PasswordResetEmail>().Where(e => e.EmailTo == emailTo).ToList();

    public Task SendEmailVerificationEmailAsync(EmailVerificationEmail emailVerificationEmail, CancellationToken cancellationToken = default) =>
        Record(emailVerificationEmail);

    public Task SendPasswordResetEmailAsync(PasswordResetEmail passwordResetEmail, CancellationToken cancellationToken = default) =>
        Record(passwordResetEmail);

    public Task SendWelcomeEmailAsync(WelcomeEmail welcomeEmail, CancellationToken cancellationToken = default) => Record(welcomeEmail);

    public Task SendPasswordChangedEmailAsync(PasswordChangedEmail passwordChangedEmail, CancellationToken cancellationToken = default) =>
        Record(passwordChangedEmail);

    public Task SendNotificationEmailAsync(NotificationEmail notificationEmail, CancellationToken cancellationToken = default) =>
        Record(notificationEmail);

    public Task SendBookingReservedEmailAsync(BookingReservedEmail bookingReservedEmail, CancellationToken cancellationToken = default) =>
        Record(bookingReservedEmail);

    public Task SendBookingConfirmedEmailAsync(BookingConfirmedEmail bookingConfirmedEmail, CancellationToken cancellationToken = default) =>
        Record(bookingConfirmedEmail);

    public Task SendBookingRejectedEmailAsync(BookingRejectedEmail bookingRejectedEmail, CancellationToken cancellationToken = default) =>
        Record(bookingRejectedEmail);

    public Task SendBookingCompletedEmailAsync(BookingCompletedEmail bookingCompletedEmail, CancellationToken cancellationToken = default) =>
        Record(bookingCompletedEmail);

    public Task SendBookingCancelledEmailAsync(BookingCancelledEmail bookingCancelledEmail, CancellationToken cancellationToken = default) =>
        Record(bookingCancelledEmail);

    /// <summary>
    /// Extracts the <c>token</c> query parameter of an e-mailed link.
    /// </summary>
    public static string TokenFromLink(string link)
    {
        string query = new Uri(link).Query.TrimStart('?');

        string token = query.Split('&')
            .Select(part => part.Split('=', 2))
            .Single(pair => pair[0] == "token")[1];

        return Uri.UnescapeDataString(token);
    }

    private Task Record(object email)
    {
        _emails.Enqueue(email);
        return Task.CompletedTask;
    }
}
