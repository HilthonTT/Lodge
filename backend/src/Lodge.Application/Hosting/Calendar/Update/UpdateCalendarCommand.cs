using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Hosting.Calendar.Update;

/// <summary>
/// Blocks/unblocks nights and/or sets/clears custom nightly prices for every night from <paramref name="From"/> to
/// <paramref name="To"/> (inclusive, at most 366 days, not in the past). Booked nights may be blocked too; that has no
/// effect on the existing bookings.
/// </summary>
/// <param name="ApartmentId">The listing.</param>
/// <param name="From">The first night.</param>
/// <param name="To">The last night (inclusive).</param>
/// <param name="Blocked">True blocks, false unblocks, null leaves blocks unchanged.</param>
/// <param name="Price">A custom nightly price, or null to leave prices unchanged.</param>
/// <param name="ClearPrice">True removes the custom prices.</param>
public sealed record UpdateCalendarCommand(
    Guid ApartmentId,
    DateOnly From,
    DateOnly To,
    bool? Blocked,
    decimal? Price,
    bool ClearPrice) : ICommand;
