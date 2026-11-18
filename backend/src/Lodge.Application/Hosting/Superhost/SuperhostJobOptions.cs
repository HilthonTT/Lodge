using System.ComponentModel.DataAnnotations;

namespace Lodge.Application.Hosting.Superhost;

/// <summary>
/// Settings of the daily Superhost job (section <c>BackgroundJobs:Superhost</c>).
/// </summary>
public sealed class SuperhostJobOptions
{
    public const string SectionName = "BackgroundJobs:Superhost";

    /// <summary>
    /// Hangfire cron expression (UTC). Defaults to daily at 04:00.
    /// </summary>
    [Required]
    public string Schedule { get; set; } = "0 4 * * *";
}
