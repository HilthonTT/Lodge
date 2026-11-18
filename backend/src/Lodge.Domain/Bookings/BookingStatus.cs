namespace Lodge.Domain.Bookings;

/// <summary>
/// Represents the booking statuses (serialized as their numeric values).
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// The dates are held and the guest has to pay.
    /// </summary>
    Reserved = 1,
    Confirmed = 2,
    Rejected = 3,
    Cancelled = 4,
    Completed = 5,

    /// <summary>
    /// A request to book a listing without instant booking; the dates are held until the host responds.
    /// </summary>
    PendingApproval = 6,

    /// <summary>
    /// A request the host did not answer in time.
    /// </summary>
    Expired = 7,
}
