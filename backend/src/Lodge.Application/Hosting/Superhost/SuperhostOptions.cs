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
    /// The host's cancellation rate must be strictly below this fraction. Defaults to 0.10 (10%).
    /// The rate is (declined or rejected bookings + bookings the host cancelled) / bookings the host accepted or
    /// declined (instant bookings count as accepted). Pending, expired and guest-cancelled requests that were never
    /// accepted are not counted; guest cancellations never count as cancellations.
    /// </summary>
    [Range(typeof(decimal), "0", "1")]
    public decimal MaxCancellationRate { get; set; } = SuperhostRequirements.Default.MaxCancellationRate;

    public SuperhostRequirements ToRequirements() => new(MinCompletedStays, MinAverageRating, MaxCancellationRate);
}
