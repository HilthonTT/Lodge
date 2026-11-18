namespace Lodge.Application.Bookings.Complete;

/// <summary>
/// Completes the confirmed bookings whose stay has ended.
/// </summary>
public interface ICompleteBookingsJob
{
    /// <summary>
    /// Completes every confirmed booking whose end date is before today (UTC).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of completed bookings.</returns>
    Task<int> CompleteFinishedBookingsAsync(CancellationToken cancellationToken = default);
}
