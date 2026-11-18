using System.ComponentModel.DataAnnotations;

namespace Lodge.Application.Notifications;

/// <summary>
/// Settings of the daily reminder job (section <c>BackgroundJobs:Reminders</c>).
/// </summary>
public sealed class RemindersJobOptions
{
    public const string SectionName = "BackgroundJobs:Reminders";

    /// <summary>
    /// Hangfire cron expression (UTC). Defaults to daily at 08:00.
    /// </summary>
    [Required]
    public string Schedule { get; set; } = "0 8 * * *";
}
