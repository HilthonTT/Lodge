namespace Lodge.Contracts.Bookings;

/// <summary>
/// Represents the booking duration.
/// </summary>
/// <param name="StartDate">The start date.</param>
/// <param name="EndDate">The end date.</param>
public sealed record BookingDuration(DateTime StartDate, DateTime EndDate);
