using Lodge.Application.Abstractions.Emails;
using Lodge.Contracts.Emails;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Lodge.Infrastructure.Emails;

/// <summary>
/// Sends e-mails over SMTP (MailKit). Without an SMTP server configured the e-mail is logged instead.
/// </summary>
internal sealed partial class EmailService(IOptions<MailOptions> options, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendEmailAsync(MailRequest mailRequest, CancellationToken cancellationToken = default)
    {
        MailOptions mailOptions = options.Value;

        if (!mailOptions.IsConfigured)
        {
            LogEmailNotSent(logger, mailRequest.EmailTo, mailRequest.Subject);
            return;
        }

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(mailOptions.SenderDisplayName, mailOptions.SenderEmail));
        message.To.Add(MailboxAddress.Parse(mailRequest.EmailTo));
        message.Subject = mailRequest.Subject;
        message.Body = new TextPart(TextFormat.Text) { Text = mailRequest.Body };

        using var smtpClient = new SmtpClient();

        await smtpClient.ConnectAsync(
            mailOptions.SmtpServer!,
            mailOptions.SmtpPort,
            SecureSocketOptions.StartTlsWhenAvailable,
            cancellationToken);

        if (!string.IsNullOrEmpty(mailOptions.SmtpPassword))
        {
            await smtpClient.AuthenticateAsync(
                mailOptions.SmtpUserName ?? mailOptions.SenderEmail,
                mailOptions.SmtpPassword,
                cancellationToken);
        }

        await smtpClient.SendAsync(new SendRequest(message), cancellationToken);
        await smtpClient.DisconnectAsync(true, cancellationToken);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "SMTP is not configured; e-mail '{Subject}' to {Recipient} was not sent")]
    private static partial void LogEmailNotSent(ILogger logger, string recipient, string subject);
}
