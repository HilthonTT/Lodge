using System.ComponentModel.DataAnnotations;

namespace Lodge.Application.Bookings.Complete;

/// <summary>
/// Settings of the job that completes finished stays (section <c>BackgroundJobs:CompleteBookings</c>).
/// </summary>
public sealed class CompleteBookingsOptions
{
    public const string SectionName = "BackgroundJobs:CompleteBookings";

    /// <summary>
    /// Hangfire cron expression (UTC). Defaults to daily at 03:00.
    /// </summary>
    [Required]
    public string Schedule { get; set; } = "0 3 * * *";
}
