using System.ComponentModel.DataAnnotations;

namespace Lodge.Persistence.Outbox;

/// <summary>
/// Outbox processing settings (section <c>BackgroundJobs:Outbox</c>).
/// </summary>
public sealed class OutboxOptions
{
    public const string SectionName = "BackgroundJobs:Outbox";

    /// <summary>
    /// Hangfire cron expression (6 fields = with seconds). Defaults to every 10 seconds.
    /// </summary>
    [Required]
    public string Schedule { get; set; } = "0/10 * * * * *";

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 20;
}
