namespace Lodge.Domain.Apartments;

/// <summary>
/// The length of a flexible-date search ("a weekend / a week / a month in December or January").
/// </summary>
public enum FlexibleStayLength
{
    /// <summary>
    /// Friday to Sunday (2 nights).
    /// </summary>
    Weekend = 1,

    /// <summary>
    /// 7 nights, any start day.
    /// </summary>
    Week = 2,

    /// <summary>
    /// 28 nights, any start day.
    /// </summary>
    Month = 3,
}
