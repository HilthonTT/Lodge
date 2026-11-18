namespace Lodge.Domain.Bookings;

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
