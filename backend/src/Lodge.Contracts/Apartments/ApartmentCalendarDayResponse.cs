namespace Lodge.Contracts.Apartments;

/// <summary>
/// One night of a listing's public calendar.
/// </summary>
/// <param name="Date">The date the night starts on.</param>
/// <param name="Available">Whether the night can be booked (not booked, not blocked, not in the past).</param>
/// <param name="Price">The nightly price (override, weekend or base price).</param>
public sealed record ApartmentCalendarDayResponse(DateOnly Date, bool Available, decimal Price);
