using System.ComponentModel.DataAnnotations;

namespace Lodge.Application.Experiences;

/// <summary>
/// Settings of the experience jobs (section <c>BackgroundJobs:Experiences</c>).
/// </summary>
public sealed class ExperienceJobOptions
{
    public const string SectionName = "BackgroundJobs:Experiences";

    /// <summary>
    /// Hangfire cron expression (UTC) of the completion job. Defaults to daily at 03:15.
    /// </summary>
    [Required]
    public string CompleteSchedule { get; set; } = "15 3 * * *";

    /// <summary>
    /// Hangfire cron expression (UTC) of the job that releases unpaid holds. Defaults to every 15 minutes.
    /// </summary>
    [Required]
    public string ExpireHoldsSchedule { get; set; } = "*/15 * * * *";

    /// <summary>
    /// How long spots stay held for an unpaid booking. Defaults to 60 minutes.
    /// </summary>
    [Range(5, 24 * 60)]
    public int PaymentHoldMinutes { get; set; } = 60;
}
