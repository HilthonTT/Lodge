namespace Lodge.Contracts.Hosting;

/// <summary>
/// Blocks/unblocks nights and/or sets/clears custom nightly prices for every night from <paramref name="From"/> up to
/// and including <paramref name="To"/>.
/// </summary>
/// <param name="From">The first night.</param>
/// <param name="To">The last night (inclusive).</param>
/// <param name="Blocked">True blocks, false unblocks, null leaves the blocks unchanged.</param>
/// <param name="Price">A custom nightly price, or null to leave prices unchanged.</param>
/// <param name="ClearPrice">True removes the custom prices (back to the base or weekend price).</param>
public sealed record UpdateCalendarRequest(
    DateOnly From,
    DateOnly To,
    bool? Blocked = null,
    decimal? Price = null,
    bool ClearPrice = false);
