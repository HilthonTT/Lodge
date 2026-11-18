namespace Lodge.Infrastructure.Emails;

/// <summary>
/// SMTP settings (section <c>Mail</c>). When <see cref="SmtpServer"/> is empty, e-mails are only logged.
/// </summary>
public sealed class MailOptions
{
    public const string SectionName = "Mail";

    public string SenderDisplayName { get; set; } = "Lodge";

    public string SenderEmail { get; set; } = "noreply@lodge.dev";

    public string? SmtpServer { get; set; }

    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// The SMTP user name. Defaults to <see cref="SenderEmail"/> when only a password is configured.
    /// </summary>
    public string? SmtpUserName { get; set; }

    /// <summary>
    /// The SMTP password (secret). When empty, the client does not authenticate (e.g. Mailpit in Docker).
    /// </summary>
    public string? SmtpPassword { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SmtpServer);
}
