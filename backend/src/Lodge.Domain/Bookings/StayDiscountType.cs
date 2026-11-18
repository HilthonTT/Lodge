namespace Lodge.Domain.Bookings;

/// <summary>
/// The discount applied to long stays.
/// </summary>
public enum StayDiscountType
{
    None = 0,

    /// <summary>
    /// The weekly discount (stays of at least 7 nights).
    /// </summary>
    Weekly = 1,

    /// <summary>
    /// The monthly discount (stays of at least 28 nights).
    /// </summary>
    Monthly = 2,
}
