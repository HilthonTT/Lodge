namespace Lodge.Domain.Messaging;

/// <summary>
/// The stable codes of system messages (<see cref="Message.SystemEvent"/>), which the frontend localises. The message
/// body only holds an English fallback.
/// </summary>
public static class SystemEvents
{
    /// <summary>
    /// The guest requested to book a listing that needs the host's approval.
    /// </summary>
    public const string BookingRequested = "BookingRequested";

    /// <summary>
    /// The guest booked an instant-book listing (the dates are held until payment).
    /// </summary>
    public const string BookingReserved = "BookingReserved";

    public const string BookingAccepted = "BookingAccepted";

    public const string BookingDeclined = "BookingDeclined";

    public const string BookingExpired = "BookingExpired";

    /// <summary>
    /// The guest paid: the booking is confirmed.
    /// </summary>
    public const string BookingConfirmed = "BookingConfirmed";

    public const string BookingCancelledByGuest = "BookingCancelledByGuest";

    public const string BookingCancelledByHost = "BookingCancelledByHost";

    public const string BookingDatesChanged = "BookingDatesChanged";

    public static readonly IReadOnlyList<string> All =
    [
        BookingRequested,
        BookingReserved,
        BookingAccepted,
        BookingDeclined,
        BookingExpired,
        BookingConfirmed,
        BookingCancelledByGuest,
        BookingCancelledByHost,
        BookingDatesChanged,
    ];
}
