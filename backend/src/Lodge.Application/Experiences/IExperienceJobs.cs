namespace Lodge.Application.Experiences;

/// <summary>
/// The recurring experience jobs.
/// </summary>
public interface IExperienceJobs
{
    /// <summary>
    /// Completes the confirmed bookings whose session has ended (daily).
    /// </summary>
    /// <returns>The number of completed bookings.</returns>
    Task<int> CompletePastSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the spots of unpaid bookings older than <see cref="ExperienceJobOptions.PaymentHoldMinutes"/>, and of
    /// unpaid bookings whose session has started.
    /// </summary>
    /// <returns>The number of released bookings.</returns>
    Task<int> ExpireUnpaidHoldsAsync(CancellationToken cancellationToken = default);
}
