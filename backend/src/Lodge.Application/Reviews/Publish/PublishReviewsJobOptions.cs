using System.ComponentModel.DataAnnotations;

namespace Lodge.Application.Reviews.Publish;

/// <summary>
/// Settings of the daily review publishing job (section <c>BackgroundJobs:PublishReviews</c>).
/// </summary>
public sealed class PublishReviewsJobOptions
{
    public const string SectionName = "BackgroundJobs:PublishReviews";

    /// <summary>
    /// Hangfire cron expression (UTC). Defaults to daily at 03:30.
    /// </summary>
    [Required]
    public string Schedule { get; set; } = "30 3 * * *";
}
