namespace Lodge.Contracts.Apartments;

/// <summary>
/// One night of a listing's calendar as the host sees it.
/// </summary>
/// <param name="Date">The date the night starts on.</param>
/// <param name="Available">Whether guests can book the night (not booked, not blocked, not in the past).</param>
/// <param name="Blocked">Whether the host blocked the night.</param>
/// <param name="Booked">Whether an active booking or booking request holds the night.</param>
/// <param name="Price">The nightly price (override, weekend or base price).</param>
/// <param name="CustomPrice">Whether <paramref name="Price"/> is a per-night override.</param>
public sealed record HostCalendarDayResponse(
    DateOnly Date,
    bool Available,
    bool Blocked,
    bool Booked,
    decimal Price,
    bool CustomPrice);
