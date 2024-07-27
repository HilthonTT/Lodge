﻿namespace Lodge.Infrastructure.Emails.Settings;

/// <summary>
/// Represents the main settings.
/// </summary>
internal sealed class MailSettings
{
    public const string SettingsKey = "Mail";

    /// <summary>
    /// Gets or sets the email sender's display name.
    /// </summary>
    public string SenderDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email sender.
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP password.
    /// </summary>
    public string SmtpPassword { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP server.
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP port.
    /// </summary>
    public int SmtpPort { get; set; }
}