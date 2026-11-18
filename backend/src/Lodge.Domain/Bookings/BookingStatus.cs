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

/// <summary>
/// Groups of booking statuses.
/// </summary>
public static class BookingStatuses
{
    /// <summary>
    /// Statuses whose nights are unavailable to other guests.
    /// </summary>
    public static readonly IReadOnlyList<BookingStatus> Active =
    [
        BookingStatus.PendingApproval,
        BookingStatus.Reserved,
        BookingStatus.Confirmed,
        BookingStatus.Completed,
    ];

    /// <summary>
    /// The numeric values of <see cref="Active"/>, for SQL parameters.
    /// </summary>
    public static readonly int[] ActiveValues = [.. Active.Select(status => (int)status)];

    /// <summary>
    /// Statuses of stays that are still going to happen (used to protect listings from deletion).
    /// </summary>
    public static readonly IReadOnlyList<BookingStatus> Upcoming =
    [
        BookingStatus.PendingApproval,
        BookingStatus.Reserved,
        BookingStatus.Confirmed,
    ];
}
