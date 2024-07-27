using Lodge.Application.Abstractions.Emails;
using Lodge.Contracts.Emails;
using Lodge.Infrastructure.Emails.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Lodge.Infrastructure.Emails;

/// <summary>
/// Represents the email service.
/// </summary>
/// <param name="options">The mail settings options.</param>
internal sealed class EmailService(IOptions<MailSettings> options) : IEmailService
{
    private readonly MailSettings _mailSettings = options.Value;

    /// <inheritdoc />
    public async Task SendEmailAsync(MailRequest mailRequest, CancellationToken cancellationToken = default)
    {
        var email = new MimeMessage
        {
            From =
            {
                new MailboxAddress(_mailSettings.SenderDisplayName, _mailSettings.SenderEmail)
            },
            To =
            {
                MailboxAddress.Parse(mailRequest.EmailTo)
            },
            Subject = mailRequest.Subject,
            Body = new TextPart(TextFormat.Text)
            {
                Text = mailRequest.Body
            }
        };

        using var smtpClient = new SmtpClient();

        await smtpClient.ConnectAsync(
            _mailSettings.SmtpServer,
            _mailSettings.SmtpPort, 
            SecureSocketOptions.StartTls, 
            cancellationToken);

        await smtpClient.AuthenticateAsync(
            _mailSettings.SenderEmail,
            _mailSettings.SmtpPassword, 
            cancellationToken);

        await smtpClient.SendAsync(email, cancellationToken);

        await smtpClient.DisconnectAsync(true, cancellationToken);
    }
}
