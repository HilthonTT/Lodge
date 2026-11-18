namespace Lodge.Contracts.Bookings;

/// <summary>
/// Moves a pending or reserved booking to new dates.
/// </summary>
public sealed record ChangeBookingDatesRequest(DateOnly StartDate, DateOnly EndDate);
