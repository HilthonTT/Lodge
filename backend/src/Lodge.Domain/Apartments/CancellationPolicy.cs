namespace Lodge.Domain.Apartments;

/// <summary>
/// The cancellation policy of a listing (stored and serialized by name). It is snapshotted on every booking.
/// </summary>
/// <remarks>
/// Refund rules, by the number of days between the cancellation date and the check-in date (UTC dates):
/// <list type="bullet">
/// <item><see cref="Flexible"/>: at least 1 day → 100%, otherwise 0%.</item>
/// <item><see cref="Moderate"/>: at least 5 days → 100%, otherwise 50%.</item>
/// <item><see cref="Strict"/>: at least 14 days → 100%, 7 to 13 days → 50%, otherwise 0%.</item>
/// </list>
/// See <see cref="Bookings.RefundPolicy"/> for what the percentage applies to.
/// </remarks>
public enum CancellationPolicy
{
    Flexible = 1,
    Moderate = 2,
    Strict = 3,
}
