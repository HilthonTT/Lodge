using System.ComponentModel.DataAnnotations;

namespace Lodge.Application.Bookings.Expire;

/// <summary>
/// Settings of the job that expires unanswered booking requests (section <c>BackgroundJobs:ExpireBookingRequests</c>).
/// </summary>
public sealed class ExpireBookingRequestsOptions
{
    public const string SectionName = "BackgroundJobs:ExpireBookingRequests";

    /// <summary>
    /// Hangfire cron expression (UTC). Defaults to every 15 minutes.
    /// </summary>
    [Required]
    public string Schedule { get; set; } = "*/15 * * * *";

    /// <summary>
    /// How long a host has to answer a booking request. Defaults to 24 hours.
    /// </summary>
    [Range(1, 24 * 30)]
    public int ExpiryHours { get; set; } = 24;
}
