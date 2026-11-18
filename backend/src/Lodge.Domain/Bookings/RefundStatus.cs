namespace Lodge.Domain.Bookings;

/// <summary>
/// The status of the refund of a cancelled booking (stored and serialized by name).
/// </summary>
public enum RefundStatus
{
    /// <summary>
    /// Nothing to refund (unpaid, not cancelled, or a 0% refund).
    /// </summary>
    None = 0,

    /// <summary>
    /// The refund has been requested from the payment provider.
    /// </summary>
    Pending = 1,

    Succeeded = 2,

    Failed = 3,

    /// <summary>
    /// The booking was paid before payment tracking existed (e.g. seeded data): the refund has to be made manually.
    /// </summary>
    Manual = 4,
}
