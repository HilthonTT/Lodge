using System.ComponentModel.DataAnnotations;

namespace Lodge.Application.Payouts;

/// <summary>
/// Settings of the daily payout job (section <c>BackgroundJobs:Payouts</c>).
/// </summary>
public sealed class PayoutJobOptions
{
    public const string SectionName = "BackgroundJobs:Payouts";

    /// <summary>
    /// Hangfire cron expression (UTC). Defaults to daily at 05:00.
    /// </summary>
    [Required]
    public string Schedule { get; set; } = "0 5 * * *";
}
