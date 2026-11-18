using System.ComponentModel.DataAnnotations;
using Lodge.Domain.Users;

namespace Lodge.Application.Hosting.Superhost;

/// <summary>
/// The Superhost requirements (section <c>Hosting:Superhost</c>).
/// </summary>
public sealed class SuperhostOptions
{
    public const string SectionName = "Hosting:Superhost";

    /// <summary>
    /// The minimum number of completed stays across the host's listings. Defaults to 5.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MinCompletedStays { get; set; } = SuperhostRequirements.Default.MinCompletedStays;

    /// <summary>
    /// The minimum average review rating (1 to 5). Defaults to 4.8.
    /// </summary>
    [Range(typeof(decimal), "0", "5")]
    public decimal MinAverageRating { get; set; } = SuperhostRequirements.Default.MinAverageRating;

    /// <summary>
    /// The host's cancellation rate (declined or rejected bookings / bookings) must be strictly below this fraction.
    /// Defaults to 0.10 (10%).
    /// </summary>
    [Range(typeof(decimal), "0", "1")]
    public decimal MaxCancellationRate { get; set; } = SuperhostRequirements.Default.MaxCancellationRate;

    public SuperhostRequirements ToRequirements() => new(MinCompletedStays, MinAverageRating, MaxCancellationRate);
}

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
