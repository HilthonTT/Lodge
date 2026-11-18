namespace Lodge.Contracts.Bookings;

/// <summary>
/// One step of a refund timeline.
/// </summary>
/// <param name="Until">The last day (inclusive, UTC) of this step, or null for the last step (until check-in).</param>
/// <param name="RefundPercent">The refund percentage of this step.</param>
public sealed record RefundTimelineEntryResponse(DateOnly? Until, int RefundPercent);
