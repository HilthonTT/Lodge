namespace Lodge.Application.Payouts;

/// <summary>
/// Pays the due host payouts.
/// </summary>
public interface IPayDuePayoutsJob
{
    /// <summary>
    /// Pays every due payout (scheduled or on hold, dated today or earlier): a Stripe transfer to the host's connected
    /// account, or a simulated transfer. Payouts of hosts without an active payout account are put on hold.
    /// </summary>
    /// <returns>The number of paid payouts.</returns>
    Task<int> PayDueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pays the due payouts of one host (e.g. once they connected a payout account).
    /// </summary>
    Task<int> PayDueForHostAsync(Guid hostId, CancellationToken cancellationToken = default);
}
